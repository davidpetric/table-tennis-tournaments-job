import dotenv from 'dotenv';
import { chromium } from 'playwright';
import Database from 'better-sqlite3';
dotenv.config();

// Initialize SQLite database with WAL mode for better concurrent access
const db = new Database('tournaments.db', {
    verbose: console.log
});
db.pragma('journal_mode = WAL');

// Create tournaments table if it doesn't exist
db.prepare(`
  CREATE TABLE IF NOT EXISTS tournaments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    forumUrl TEXT UNIQUE,
    tournamentName TEXT,
    city TEXT,
    venue TEXT,
    createdAt DATETIME DEFAULT CURRENT_TIMESTAMP
  )
`).run();

async function sendDiscordNotification(content) {
    await fetch(process.env.DISCORD_WEBHOOK_URL, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            content: content
        })
    });
}

async function notifySubscribers(tournament) {
    const content = `${tournament.forumUrl}`;

    await sendDiscordNotification(content);
}

async function scrape() {
    let runMessages = [];

    try {
        const browser = await chromium.launch({ headless: true });
        const context = await browser.newContext();
        const page = await context.newPage();
        const userAgent = await page.evaluate(() => navigator.userAgent);
        console.log('Current user agent:', userAgent);

        await page.goto("https://www.amatur.ro/tenisdemasa/tt");
        await page.waitForLoadState('domcontentloaded', { timeout: 5000 });
        await page.waitForSelector('#load_data');

        const tournaments = await page.$$eval('#load_data .l1.lx', (elements) => {
            return elements.map(el => {
                // Get tournament ID
                const id = el.querySelector('.idt')?.textContent;
                // Get Google Maps link
                const mapsLink = el.querySelector('.w2.bo a')?.href;
                // Get tournament name
                const tournamentName = el.querySelector('.d2d')?.textContent.trim();
                // Get forum URL
                const forumUrl = el.querySelector('.w3.bo a')?.href;
                // Get location information
                const locationElement = el.querySelector('.t2l');
                let city = '';
                let venue = '';

                if (locationElement) {
                    const locationText = locationElement.textContent.trim();
                    const match = locationText.match(/([^(]+)\s*\(([^)]+)\)/);
                    if (match) {
                        function removeDiacritics(str) {
                            return str.normalize('NFD')
                                .replace(/[\u0300-\u036f]/g, '')
                                .replace(/[ăâ]/g, 'a')
                                .replace(/[îí]/g, 'i')
                                .replace(/[șş]/g, 's')
                                .replace(/[țţ]/g, 't')
                                .replace(/[Ăâ]/g, 'A')
                                .replace(/[Îí]/g, 'I')
                                .replace(/[Șş]/g, 'S')
                                .replace(/[Țţ]/g, 'T');
                        };
                        [, city, venue] = match;
                        city = removeDiacritics(city.trim());
                        venue = removeDiacritics(venue.trim());
                    }
                }
                return {
                    forumUrl,
                    tournamentName,
                    city,
                    venue,
                };
            });
        });

        // Prepare statements
        const checkTournament = db.prepare('SELECT * FROM tournaments WHERE forumUrl = ?');

        const insertTournament = db.prepare(`
            INSERT INTO tournaments (forumUrl, tournamentName, city, venue)
            VALUES (@forumUrl, @tournamentName, @city, @venue)
        `);

        for (const tournament of tournaments) {
            // Check if tournament exists
            const existingTournament = checkTournament.get(tournament.forumUrl);

            if (!existingTournament) {
                // New tournament
                insertTournament.run(tournament);
                await notifySubscribers(tournament);
            } else {
                console.log("Turneul deja exista", existingTournament);
            }
        }

        runMessages.push(`Run completed with success: date: ${new Date().toDateString()} -  time: ${new Date().toLocaleTimeString()} `)

        await browser.close();

    } catch (error) {
        runMessages.push(`Error occurred: ${error}`)

    } finally {
        db.close();

        await sendDiscordNotification(runMessages.join(", "));
    }
}

scrape();