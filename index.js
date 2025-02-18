import { chromium } from 'playwright';
import { PrismaClient } from '@prisma/client';

// const prisma = new PrismaClient();

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

const scrape = async () => {
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
                const forumLink = el.querySelector('.w3.bo a')?.href;

                return {
                    id,
                    forumUrl: forumLink
                };
            });
        });

        const data = JSON.stringify(tournaments, null, 2);
        console.log(data);

        for (const tournament of tournaments) {
            // // Check if tournament exists
            // const existingTournament = await prisma.tournament.findUnique({
            //     where: { id: tournament.id }
            // });

            // if (!existingTournament) {
            //     // New tournament
            //     await prisma.tournament.create({
            //         data: tournament
            //     });
            await notifySubscribers(tournament);
            // } else {
            //     console.log("Turneul deja exista", existingTournament);
            // }
        }

        await browser.close();
    } catch (error) {
        console.error(error);

    } finally {
        //  await prisma.$disconnect();
    }
}

scrape();