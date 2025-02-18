-- CreateTable
CREATE TABLE "Tournament" (
    "forumUrl" TEXT NOT NULL,
    "tournamentName" TEXT NOT NULL,
    "city" TEXT,
    "venue" TEXT,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- CreateIndex
CREATE UNIQUE INDEX "Tournament_forumUrl_key" ON "Tournament"("forumUrl");

-- CreateIndex
CREATE UNIQUE INDEX "Tournament_tournamentName_key" ON "Tournament"("tournamentName");
