import dotenv from 'dotenv';
import { chromium } from 'playwright';
import { PrismaClient } from '@prisma/client';

dotenv.config();

const prisma = new PrismaClient();

async function sendDiscordNotification(webhook_url, tournament) {
    await fetch(webhook_url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            content: `${tournament.forumUrl}`
        })
    });
}

async function notifySubscribers(tournament) {
    await sendDiscordNotification(process.env.DISCORD_WEBHOOK_URL, tournament);
}


async function scrape() {
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

        const data = JSON.stringify(tournaments, null, 2);
        console.log(data);

        for (const tournament of tournaments) {
            // Check if tournament exists
            const existingTournament = await prisma.tournament.findUnique({
                where: { forumUrl: tournament.forumUrl }
            });

            if (!existingTournament) {
                // New tournament
                await prisma.tournament.create({
                    data: tournament
                });

                await notifySubscribers(tournament);
            } else {
                console.log("Turneul deja exista", existingTournament);
            }
        }

        await browser.close();
    } catch (error) {
        console.error(error);

    } finally {
        await prisma.$disconnect();
    }
}

scrape();