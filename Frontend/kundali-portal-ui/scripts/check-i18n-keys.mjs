import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const localeDirectory = path.resolve(scriptDirectory, '../public/i18n');
const languages = ['en', 'hi', 'kn'];

async function readLocale(language) {
  const filePath = path.join(localeDirectory, `${language}.json`);
  return JSON.parse(await readFile(filePath, 'utf8'));
}

function collectPaths(value, prefix = '', paths = new Set()) {
  if (Array.isArray(value)) {
    value.forEach((item, index) => collectPaths(item, `${prefix}[${index}]`, paths));
  } else if (value !== null && typeof value === 'object') {
    for (const [key, child] of Object.entries(value)) {
      const currentPath = prefix ? `${prefix}.${key}` : key;
      paths.add(currentPath);
      collectPaths(child, currentPath, paths);
    }
  }

  return paths;
}

const locales = Object.fromEntries(await Promise.all(
  languages.map(async language => [language, await readLocale(language)]),
));
const pathsByLanguage = Object.fromEntries(
  languages.map(language => [language, collectPaths(locales[language])]),
);
const referencePaths = pathsByLanguage.en;
let hasErrors = false;

for (const language of languages.filter(language => language !== 'en')) {
  const missing = [...referencePaths].filter(key => !pathsByLanguage[language].has(key)).sort();
  const extra = [...pathsByLanguage[language]].filter(key => !referencePaths.has(key)).sort();

  if (missing.length === 0 && extra.length === 0) {
    console.log(`${language}: ${referencePaths.size} keys match English.`);
    continue;
  }

  hasErrors = true;
  console.error(`${language}: ${missing.length} missing, ${extra.length} extra key(s).`);
  for (const key of missing) console.error(`  missing: ${key}`);
  for (const key of extra) console.error(`  extra:   ${key}`);
}

if (hasErrors) process.exitCode = 1;
