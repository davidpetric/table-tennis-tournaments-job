import { chromium } from 'playwright';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

async function sendDiscordNotification(webhook_url, tournament, isNew = true) {
    const embed = {
        title: tournament.tournamentName,
        description: `${isNew ? 'Turneu nou' : 'Turneu actualizat'} ${tournament.location}`,
        fields: [
            {
                name: 'Data',
                value: `${tournament.date} ${tournament.year}`,
                inline: true
            },
            {
                name: 'Locatie',
                value: tournament.venue,
                inline: true
            },
            {
                name: 'Categori',
                value: tournament.categories
            },
            {
                name: 'Zile',
                value: tournament.days
            },
            {
                name: 'ForumUrl',
                value: tournament.forumUrl
            }
        ],
        url: tournament.forumUrl,
        color: isNew ? 0x00ff00 : 0xffaa00
    };

    await fetch(webhook_url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ embeds: [embed] })
    });
}

async function notifySubscribers(tournament, isNew = true) {
    // Get all subscribers for this location
    const subscribers = await prisma.locationSubscription.findMany({
        where: {
            location: tournament.location
        },
        include: {
            user: true
        }
    });

    // Send notification to each subscriber's webhook
    for (const sub of subscribers) {
        await sendDiscordNotification(sub.user.webhookUrl, tournament, isNew);
    }
}

const scrape = async () => {
    try {
        const browser = await chromium.launch({ headless: false, slowMo: 50 });

        const context = await browser.newContext();

        const page = await context.newPage();

        const userAgent = await page.evaluate(() => navigator.userAgent);
        console.log('Current user agent:', userAgent);

        await page.goto("https://www.amatur.ro/tenisdemasa/tt");

        await page.waitForSelector('#load_data');

        const tournaments = await page.$$eval('#load_data .l1.lx', (elements) => {
            return elements.map(el => {
                // Get tournament ID
                const id = el.querySelector('.idt')?.textContent;

                // Get date
                const date = el.querySelector('.w2.bo b')?.textContent;
                const year = el.querySelector('.w2.bo')?.textContent.match(/\d{4}/)?.[0];

                // Get Google Maps link
                const mapsLink = el.querySelector('.w2.bo a')?.href;

                // Get location and venue details
                const locationElement = el.querySelector('.t2l span');
                const location = locationElement?.childNodes[0]?.textContent.trim();
                const venue = locationElement?.querySelector('span')?.textContent.replace(/[\(\)]/g, '').trim();

                // Get tournament name
                const tournamentName = el.querySelector('.d2d')?.textContent.trim();

                // Get forum URL
                const forumLink = el.querySelector('.w3.bo a')?.href;

                // Get tournament days schedule
                const days = Array.from(el.querySelectorAll('.day'))
                    .map(day => day.textContent.trim())
                    .join(" / ");

                // Get categories
                const categories = Array.from(el.querySelectorAll('.w4.bo .sys h2'))
                    .map(cat => cat.textContent.trim())
                    .filter(cat => cat)
                    .join(",");

                return {
                    id,
                    date,
                    year,
                    location,
                    venue,
                    tournamentName,
                    days,
                    categories,
                    mapsLink,
                    forumUrl: forumLink
                };
            });
        });

        const data = JSON.stringify(tournaments, null, 2);
        console.log(data);

        for (const tournament of tournaments) {
            // Check if tournament exists
            const existingTournament = await prisma.tournament.findUnique({
                where: { id: tournament.id }
            });

            if (!existingTournament) {
                // New tournament
                await prisma.tournament.create({
                    data: tournament
                });
                await notifySubscribers(tournament, true);
            } else {
                // Check for significant changes
                const hasChanges = JSON.stringify(existingTournament) !== JSON.stringify(tournament);
                if (hasChanges) {
                    await prisma.tournament.update({
                        where: { id: tournament.id },
                        data: tournament
                    });
                    await notifySubscribers(tournament, false);
                }
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