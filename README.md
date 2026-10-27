# PriceHunt

A shipping price search. The user picks two places, a date range and some suppliers, and the prices show up on screen one by one as the suppliers answer, cheapest first. Every search and every answer is stored, and a second screen lets you look back through that history.

The backend is .NET 8 with EF Core and SQLite. The frontend is Angular 17 and lives in `frontend/`.

## Running it

You need the .NET 8 SDK and, for the frontend, Node 18.13+ or 20. There is no database server, Docker or configuration to set up. The API creates the SQLite file and applies the migrations on the first run.

Backend:

```bash
dotnet run --project backend/PriceHunt.API/PriceHunt.API
```

It listens on http://localhost:5229 and serves Swagger at `/swagger`. The database goes to `backend/PriceHunt.API/PriceHunt.API/App_Data/pricehunt.db`; set `Database:Path` to put it somewhere else. If you ran an older version of this project that did not use migrations, delete the old `pricehunt.db` first, because migrations cannot be applied to a file that already has the tables. Logs go to the console and to `logs/pricehunt-<date>.log` in the folder the API is started from.

Frontend, in a second terminal:

```bash
npm start --prefix frontend
```

The first run installs the dependencies by itself (the `prestart` script in `frontend/package.json`), so there is no separate `npm install`. After that it starts the dev server on http://localhost:4200, which calls the API on port 5229 (set in `frontend/backend/environments/`). The same works from inside `frontend/` with `npm start`.

Tests:

```bash
dotnet test backend/PriceHunt.API/PriceHunt.API.sln
cd frontend && npm run test:ci      # needs Chrome
```

## What it does

**Search** (`/search`). From and to place, from and to date, and a supplier list with every supplier ticked. Prices appear as they arrive and the rows slide into their sorted position, so the list does not jump. A progress line reads "5 of 7 suppliers responded" and finishes as Completed, Timed out or Cancelled. Starting a new search cancels the one that is running, and there is a Cancel button too. If you leave for the history page and come back, the last results and the form values are still there.

**History** (`/history`). Filters for date range, origin, destination and suppliers, and a table with Date, Route, Supplier, Status, Price and Response time. Every column sorts, and there is paging. Filtering, sorting and paging all happen on the server.

## How results are streamed, and why

The search endpoint is a POST that answers with Server-Sent Events. The response stays open and the server writes one small message each time something happens:

```
event: started
data: {"searchId":"...","suppliers":[{"id":"...","name":"Atlas Freight"}, ...]}

event: supplier
data: {"searchId":"...","supplierName":"Atlas Freight","status":"Succeeded","price":753.94,"currency":"USD","responseTimeMilliseconds":2359}

event: finished
data: {"searchId":"...","status":"TimedOut","selectedCount":7,"respondedCount":6}
```

A normal JSON response only goes out when the action has finished, which here means nothing for up to six seconds and then all seven prices at once. That is what the brief wants to avoid. .NET 8 has no built-in SSE result, so `SearchEventStreamResult` is a small custom one: it sets `text/event-stream`, turns off buffering and flushes after every event. The controller action is two lines.

I looked at the alternatives before settling on this.

- Polling adds delay and load, and the server has to hold partial results between calls.
- SignalR or WebSockets work, but they are more than a one-way feed needs.
- Returning `IAsyncEnumerable` as JSON sends a half-written array that the frontend cannot parse until the end.
- The browser's `EventSource` only does GET and reconnects on its own, which would quietly restart a search the user cancelled. A search also writes to the database, so POST is the right verb. The Angular frontend reads the stream through `HttpClient` with `reportProgress` and a small parser (`sse-parser.ts`). That way the normal interceptors still apply, and unsubscribing aborts the request.
- One request per supplier would put the six second timer and the stitching of seven answers in the browser, and the stored history and cancellation would each have to be coordinated across seven requests. Browsers on HTTP/1.1 also open only about six connections at a time. One streamed request keeps all of that in one place on the server. It costs more code: the custom result on the server and the parser on the frontend.

### What happens during a search

1. The form checks its input, and a new search cancels any running one.
2. The API validates the request again, because the browser can be bypassed. Places must exist in the `Locations` table and suppliers in the `Suppliers` table. If anything is wrong it answers 400 with every problem listed per field and stores nothing. Place names are replaced by their stored spelling, so "hamburg" becomes "Hamburg".
3. The search is saved as `Running` together with the selected suppliers.
4. All selected suppliers are asked at the same moment, each in its own task and all sharing one cancellation token. The frontend gets the `started` event first, so it can show every supplier as waiting.
5. Each answer is stored as soon as it arrives and sent as a `supplier` event. A supplier can succeed with a price, fail with an error, or never answer. An exception in one supplier only turns that supplier's row into `Failed`.
6. The search ends when every supplier has answered (`Completed`), when six seconds have passed (`TimedOut`), or when the frontend leaves or cancels (`Cancelled`). Suppliers that had not answered are stored as `TimedOut` or `Cancelled` with the time that was waited, the final status is saved, and the `finished` event closes the response. Anything still running is told to stop.
7. In the browser each `supplier` event is added to the list and the list is sorted again. Every event carries the search id, and the browser drops events that belong to another search, so a late answer to an old search can never show up in a new one.

## API

| Endpoint | What it does |
|---|---|
| `POST /api/searches` | Runs a search and streams the results. |
| `GET /api/suppliers` | Lists the seven suppliers. |
| `GET /api/locations` | The places a search can use, ten at a time by default (at most 50). `search` narrows the list and puts names that start with the text first. |
| `GET /api/history` | Stored supplier results, with filters, sorting and paging. |

Search body (leave `supplierIds` out to ask all seven):

```json
{
  "fromLocation": "Rotterdam",
  "toLocation": "Hamburg",
  "fromDate": "2026-11-01",
  "toDate": "2026-11-05",
  "supplierIds": ["..."]
}
```

History query string: `from` and `to` (inclusive dates), `supplierIds` (repeat it for several), `origin`, `destination`, `sortBy` (`date`, `route`, `supplier`, `status`, `price` or `responseTime`), `descending`, `page` and `pageSize` (1 to 100). It returns `{ items, page, pageSize, totalCount }`, one item per supplier per search, so failures and timeouts are in there too.

Validation errors come back as one 400 with every problem, for example under `errors.fromDate`. The rules: both places are required, must come from `GET /api/locations` and must differ; the from date is not in the past; the to date is not before the from date; the range is at most 90 days; and a supplier list, when given, is not empty and contains only known suppliers. The Angular form checks the same rules with the same messages.

## Database

SQLite through EF Core. The schema is built by EF Core migrations, which the API applies at startup, and the file runs in WAL mode so a search can write while the history page reads. The migrations are in `PriceHunt.Infrastructure/Persistence/Migrations`. To change the schema, edit the model and, from the repository root (the folder with this README), run `dotnet tool restore`, then `dotnet ef migrations add <Name> --project backend/PriceHunt.API/PriceHunt.Infrastructure --startup-project backend/PriceHunt.API/PriceHunt.API`. In Visual Studio's Package Manager Console the starting folder is `backend/PriceHunt.API`, so use the shorter `--project PriceHunt.Infrastructure --startup-project PriceHunt.API`. A test fails if the model and the migrations disagree, so a change cannot be forgotten.

| Table | What it holds |
|---|---|
| `Searches` | One row per search: `Id`, `Origin`, `Destination`, `FromDate`, `ToDate`, `CreatedAtUtc` (indexed) and `Status` (`Running`, `Completed`, `TimedOut`, `Cancelled`). |
| `SearchSuppliers` | The suppliers selected for a search. Key (`SearchId`, `SupplierId`), plus `SupplierName` and `SortOrder`. |
| `SupplierOutcomes` | One row per supplier per search. Key (`SearchId`, `SupplierId`), index on `SupplierId`. Columns: `Status` (`Succeeded`, `Failed`, `TimedOut`, `Cancelled`), `PriceCents` and `Currency` (empty unless there was a price), `ResponseDurationMilliseconds`, `RespondedAtUtc` (empty if the supplier never answered) and `ErrorCode`. |
| `Suppliers` | The suppliers a search can ask: `Id`, `Name` (unique) and `Behavior` (`Normal`, `FailsSometimes`, `NeverResponds`). Filled on the first run. |
| `Locations` | The places that can be chosen: `Id` and `Name` (unique, ignoring case). Filled on the first run. |

The search row is written when the search starts and each outcome as it arrives, so a crash does not lose the prices already received. On startup, any search still marked `Running` is set to `Cancelled`. `SearchSuppliers` and `SupplierOutcomes` keep their own copy of the supplier name and have no foreign key to `Suppliers`, so old history stays readable if a supplier is renamed or removed. Prices are stored as whole cents in an integer column, because the SQLite provider cannot sort `decimal` and cents avoid floating point.

## Project structure

```
backend/PriceHunt.API/
  PriceHunt.Domain          entities and value objects, no dependencies
  PriceHunt.Application     use cases, validators, and the interfaces Infrastructure implements
  PriceHunt.Infrastructure  EF Core and SQLite, the supplier simulation, dependency registration
  PriceHunt.API             controllers, middleware, API contracts, the stream result
tests/PriceHunt.Tests       unit, database and HTTP tests
frontend/                   Angular app
```

Dependencies point inward: API and Infrastructure depend on Application, and Application depends on Domain. Each layer has its own models (domain types, use case models, JSON contracts, database records) with explicit mapping between them.

The Angular app is split into `core/` (logging, the error model, HTTP interceptors, notifications), `shared/` (things used by more than one feature, such as the supplier picker, form validators and date helpers) and `features/search` and `features/history`, each with its page, components, service and store. Four interceptors run in order: `api-url` adds the API address, `correlation-id` adds an `X-Correlation-Id` header that the API writes into every log line, `request-logging` logs each request, and `api-error` turns a failed request into one `ApiError` and shows a notification for unexpected failures. Validation errors (400) are not shown as notifications; the form shows them next to the field.

## Tests

The backend tests (xUnit) cover the domain rules, the search service with a fake clock and fake suppliers (the six second limit, cancellation, a failing supplier), the validators, the database stores against a real SQLite file, and the HTTP API end to end with `WebApplicationFactory`. The frontend tests (Karma and Jasmine in headless Chrome) cover the stream parser, the stores, the validators, the autocomplete, the results list, the history table and the interceptors. There are no browser end-to-end tests; I went through the full flow by hand against the running app.

## Design decisions and trade-offs

- **Layers keep their own models.** There is no shared DTO library, because that would tie the domain to the API's JSON. The price is some mapping code.
- **Services and stores sit behind interfaces.** Controllers and tests depend on the contract, and the stores can be swapped for in-memory fakes.
- **FluentValidation, one validator per request.** Each rule sits next to the field it checks. The services call the validator themselves, so a new entry point cannot skip it.
- **Mapperly for mapping.** It generates plain mapping code at build time, so a renamed property fails the build. I first used AutoMapper, but versions before 15.1.1 have a high-severity security advisory, and the fixed versions are under a newer licence (a registered key, free for small users, or the reciprocal open-source licence). That adds a setup step to a project that should run without any. Mapperly has neither problem.
- **Suppliers and places live in the database.** Both are seeded on the first run and then read from their tables, and seeding only adds rows that are missing, so a change made in a table is kept. The suppliers are still simulated, as the brief asks, so `Behavior` is a simulation setting. With real suppliers the table would hold an endpoint instead. Places are validated on the server and loaded a page at a time.
- **Seven suppliers behind one interface.** The search service only knows `ISupplierClient`. The simulation implements it with one class, `SimulatedSupplierClient`, built from each row of `Suppliers`. Delays, failure rate and price range come from the `Simulation` section of `appsettings.json`.
- **One endpoint for both place fields.** From and To ask the same question, "which places match this text?". Each field keeps its own search text and page. That the two places must differ is a validation rule.
- **Scoped `DbContext`.** A search lives inside one HTTP request. Writes happen one after another, and the supplier calls run in parallel but never touch the database.
- **`TimeProvider` for time.** Tests advance a fake clock instead of waiting six real seconds.
- **Errors in one place.** One middleware logs each request with its correlation id, and another turns any exception into a standard problem response. Controllers have no try/catch. If a stream fails after it has started, the connection is dropped, since a status code can no longer be sent.
- **Serilog for logs.** Console and a daily file, the last 14 kept, with the correlation id on every line. Framework messages are limited to warnings.
- **Records for data, classes for behaviour.** API contracts, events, queries and value objects are records. Entities with behaviour, services and controllers are classes, and concrete types are `sealed`. The two "one of" types, `ShippingSearchUpdate` and `SupplierQuote`, are abstract records with private constructors, so their cases are closed.
- **Migrations applied at startup.** A new database is created on the first run with no extra command, and a later schema change is a new migration, not a deleted file. The cost is a folder of generated code and a design-time package in the Infrastructure project.
- **Time zone tolerance.** The API accepts yesterday as a from date, so a user whose local date is behind the server's can still search for their own today. The date picker itself only offers today onwards.

## Guide for changing the supplier search

It says what the search must do, where each piece of logic lives, and the rules that keep the code free of duplication and fast. If a change goes against one of these rules, the rule should be changed on purpose, here, first.

### The requirement

A user searches for shipping prices between two places for a date range. The server asks the selected suppliers (all seven by default) and sends each price to the browser the moment it arrives, so the screen fills in from the cheapest price up. A search lasts at most 6 seconds. One supplier fails part of the time and one never answers, and neither may hurt the others. Starting a new search, or disconnecting, stops the work in progress. Every search and every answer is stored, so the user can filter the history later by date, place and supplier.

### The search logic, in order

1. Validate the request once on the server, against the database: known places, known suppliers, sensible dates. Return every problem together.
2. Save the search as `Running` before any supplier is asked.
3. Ask all selected suppliers at the same time, each in its own task, all sharing one cancellation token. That token is cancelled by a frontend disconnect, by the 6 second limit and by the end of the search.
4. Turn each answer into one outcome (`Succeeded`, `Failed`, `TimedOut` or `Cancelled`), store it, then send it. An exception inside one supplier is that supplier's `Failed` outcome and nothing more.
5. End the search when everyone has answered (`Completed`), when time runs out (`TimedOut`) or when the caller leaves (`Cancelled`). Store the unanswered suppliers with the time waited, save the final status and send the `finished` event.
6. In the browser, add each event to one list, sort it, and ignore any event whose search id is not the current search's.

### Where each piece lives (one copy only)

| Concern | The one place | Everything else |
|---|---|---|
| Search flow, time limit, cancellation | `ShippingSearchService` | Controllers only start it and return the stream |
| What a supplier is and how it is asked | `ISupplierClient`, `SupplierCatalog` | Nothing outside Infrastructure knows a supplier's type |
| Request rules | One validator per request (`SearchRequestValidator`, `HistoryQueryValidator`, `LocationSearchQueryValidator`), shared rules in `ValidationRules` | Services call the validator; controllers do no checking |
| Error responses | The two middlewares | No try/catch in controllers or services for HTTP concerns |
| Copying between layers | Mapperly (`ApiMapper`, `HistoryMapper`) | No hand-written copy code |
| Paging | `ToPagedResultAsync` and `PagedResult` | Stores never write their own skip and take |
| Event format | `ShippingSearchUpdate` on the server, `sse-parser.ts` and `search.models.ts` in the frontend | Add an event in these places only |
| Building and sorting the result list | `search-rows.ts` (a pure function) | Components only display the rows |
| Search state | `SearchStore` | Components read signals and call store methods |
| Form rules and messages | `shared/forms/validators.ts` and `error-messages.ts` | Templates use `<app-field-error>` only |
| Status wording and colours | `StatusBadgeComponent` | Never write a status label in a template |

### Backend rules

- Keep the layers: Domain has no dependencies, Application holds the use cases and interfaces, Infrastructure holds EF Core and the simulation, API holds controllers and streaming. A new feature starts with an interface in Application.
- Never block a thread. Every I/O call is `async`, every supplier call receives the cancellation token, and nothing waits on a result with `.Result` or `.Wait()`.
- Write to the database as answers arrive, one small write each, and never inside a loop over suppliers that could run in parallel. Reads use `AsNoTracking`, filter and sort in the query and page on the server, and nothing loads a table to filter it in memory.
- A new filter or sort column needs an index if the table can grow, and a migration for it (`dotnet ef migrations add`). A test fails when the model and the migrations disagree.
- Use `TimeProvider` for time, never `DateTime.UtcNow`, so tests can move the clock.
- Add a supplier by adding a row (or an adapter behind `ISupplierClient`). The search service must not change.

### Frontend rules

- Components display and emit events. Logic goes in stores, services and pure functions, which are the parts the tests reach easily.
- Use signals and `OnPush`. Rows are tracked by supplier id and positioned with a CSS `transform`, so a new price moves a row without recreating it. The sort is one pure function.
- Read the stream through `HttpClient` so the interceptors apply, and unsubscribe to cancel. A new search unsubscribes from the old one before it starts.
- Reuse the shared pieces (`autocomplete`, `supplier-picker`, `status-badge`, `price-cell`, `field-error`, the `surface` style) before writing a new one. Debounce anything that calls the API as the user types, and cancel the previous call when a new one starts.
- Copy nothing from the backend by hand. Where the frontend must mirror a rule (the date rules), keep it in `validators.ts` and `search-rules.ts`, use the same wording as the validator, and cover it with a test.

### Definition of done

The solution builds with no warnings, the backend and frontend tests pass, a new rule has a test on the side that owns it, and nothing is repeated that the table above gives a single home. The README changes with the code.

## What I would do differently with more time

Before it goes live:

- Move to a server database such as PostgreSQL or SQL Server, since SQLite allows one writer at a time. The stores sit behind interfaces, so the change stays in Infrastructure, together with a new set of migrations. The one SQLite detail to replace is the case-insensitive `NOCASE` collation on the place and supplier names.
- Add sign-in and rate limits. Every search starts seven supplier calls and holds a connection for up to six seconds.
- Support several API servers. A search that a crash leaves as `Running` is closed when the API starts, which is the right and simplest behaviour for one instance. With more than one instance I would add a heartbeat column and close only the searches whose heartbeat has stopped.
- Build on the logging that is already there. Every line already has a correlation id and the output is structured, so the next step is to point Serilog at a central log store and add metrics and alerts for supplier response time, failure rate and timeouts.

Suppliers:

- Real suppliers plug in through `ISupplierClient`. The `Suppliers` table already lists each supplier, so the work is an endpoint and an enabled flag in the table and one adapter per supplier type. Each adapter would get its own timeout, retry and circuit breaker, so a slow supplier never affects the others.
- A short cache on the supplier list would let an operator switch a supplier off without restarting the API.

Quality:

- Let the API publish its limits (the 90 day range and the date rules), so the form reads them from one source. The form and the API already use the same rules and wording, and each has its own tests for them.
- Add contract tests between the API and the frontend, and browser end-to-end tests for the search and history flows, to run in the build.

## Use of AI

I used Claude Code (Anthropic) as an assistant during this project. The design, the decisions and the review were mine.

Where I used it:

- Drafting parts of the .NET and Angular code and the unit, database and HTTP tests, which I then reviewed and sent back for changes.
- Refactoring on my feedback: simplifying the history query, the error-handling middleware and the mapper attributes, renaming the stores, and moving validation to FluentValidation.
- Checking the project against the brief and the review criteria, and scanning the code for duplication, unused code and missing comments.
- Finding and fixing two unreliable tests by running the suite repeatedly under load.
- Drafting this README.

What I did myself:

- Chose the architecture and the trade-offs above: the layers, SQLite, SSE for streaming, suppliers and places in the database, a paged and server-searched place list, one validator per request, Mapperly and Serilog.
- Reviewed the code and decided what stayed, what was simplified and what was dropped.
- Ran the application and tried it by hand: searches with failing and silent suppliers, cancelling, the history filters, wrong input in the forms. I asked for changes to the screens as a result.

Changes were kept only after the backend and frontend tests passed, the solution built without warnings, and I had run the API and the Angular app. I read this README against what the code does.
