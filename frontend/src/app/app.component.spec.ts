import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  it('links to the search and history pages', () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const links = Array.from<HTMLAnchorElement>(fixture.nativeElement.querySelectorAll('nav a'));

    expect(links.map((link) => link.textContent?.trim())).toEqual(['Search', 'History']);
  });
});
