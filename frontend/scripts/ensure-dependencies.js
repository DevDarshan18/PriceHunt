const { existsSync } = require('fs');
const { spawnSync } = require('child_process');

const installed = existsSync('node_modules/.bin/ng') || existsSync('node_modules/.bin/ng.cmd');

if (!installed) {
  console.log('Installing the dependencies. This only happens on the first run.');
  const result = spawnSync('npm', ['install', '--include=dev'], { stdio: 'inherit', shell: true });
  process.exit(result.status ?? 1);
}
