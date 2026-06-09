/**
 * Screenshot capture script for Shopfloor Manager README
 * Run: node scripts/take-screenshots.mjs
 */
import { chromium } from 'playwright';
import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT_DIR = path.join(__dirname, '..', 'docs', 'screenshots');
fs.mkdirSync(OUT_DIR, { recursive: true });

const BASE_URL = 'http://localhost:3000';
const API_URL = 'http://localhost:5066';

async function login(page) {
  // Get JWT via API
  const resp = await fetch(`${API_URL}/api/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userLogin: 'admin', password: 'Admin@123' }),
  });
  const data = await resp.json();
  const token = data.data.token;
  const user = data.data.user;

  // Navigate to app and inject JWT into localStorage (Zustand persist key)
  await page.goto(BASE_URL);
  await page.evaluate(({ token, user }) => {
    const authState = { state: { token, user }, version: 0 };
    localStorage.setItem('auth-storage', JSON.stringify(authState));
  }, { token, user });

  return token;
}

async function shot(page, name, url, { wait = 1500, width = 1440, height = 900 } = {}) {
  await page.setViewportSize({ width, height });
  await page.goto(`${BASE_URL}${url}`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(wait);
  const file = path.join(OUT_DIR, `${name}.png`);
  await page.screenshot({ path: file, fullPage: false });
  console.log(`✓ ${name}.png`);
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  console.log('Logging in...');
  await login(page);

  // Web App screenshots
  const pages = [
    // Dashboard
    ['web-dashboard', '/dashboard'],
    // Jobs
    ['web-jobs', '/jobs'],
    // Parts
    ['web-parts', '/parts'],
    // NCRs
    ['web-ncrs', '/ncrs'],
    // Gages
    ['web-gages', '/gages'],
    // Planning
    ['web-planning', '/planning'],
    // CNC Live
    ['web-cnc', '/cnc'],
    // Master data
    ['web-master', '/master'],
    // HR
    ['web-hr', '/hr'],
  ];

  for (const [name, url] of pages) {
    try {
      await shot(page, name, url);
    } catch (e) {
      console.error(`✗ ${name}: ${e.message}`);
    }
  }

  await browser.close();
  console.log(`\nDone! Screenshots saved to: ${OUT_DIR}`);
}

main().catch(console.error);
