const express = require('express');
const cors = require('cors');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const app = express();
app.use(cors());
app.use(express.json());

// --- API KEY AUTHENTICATION ---
const API_KEY = process.env.API_KEY;
if (!API_KEY) {
    console.error('[FATAL] API_KEY environment variable is not set. Refusing to start.');
    process.exit(1);
}

function requireApiKey(req, res, next) {
    const key = req.headers['x-api-key'];
    if (!key || key !== API_KEY) {
        return res.status(401).json({ success: false, error: 'UNAUTHORIZED' });
    }
    next();
}
app.use(requireApiKey);

// --- CASINO CONFIGURATION ---

// PATCH: Expanded to a full AAA roster (8 Base Symbols, 1 Wild, 1 Scatter)
const SYMBOLS = ["Wire", "Cable", "Fan", "Drive", "Battery", "RAM", "Microchip", "Skull", "Neon", "Seven"];

// PATCH: Deep, granular Paytable for 8 base symbols
const PAYTABLE = {
    "Wire": { 3: 0.1, 4: 0.2, 5: 0.5 },      // Tier 1 (Lowest)
    "Cable": { 3: 0.1, 4: 0.3, 5: 1.0 },     // Tier 2
    "Fan": { 3: 0.2, 4: 0.4, 5: 1.2 },       // Tier 3
    "Drive": { 3: 0.2, 4: 0.5, 5: 1.5 },     // Tier 4
    "Battery": { 3: 0.5, 4: 1.0, 5: 2.0 },   // Tier 5
    "RAM": { 3: 0.8, 4: 1.5, 5: 3.0 },       // Tier 6
    "Microchip": { 3: 1.0, 4: 2.5, 5: 5.0 }, // Tier 7
    "Skull": { 3: 2.0, 4: 5.0, 5: 15.0 },    // Tier 8 (Highest Base)
};

// Define the 12 winning lines for a 5x5 grid
const LINES = [
    [0, 1, 2, 3, 4], [5, 6, 7, 8, 9], [10, 11, 12, 13, 14], [15, 16, 17, 18, 19], [20, 21, 22, 23, 24], // Horizontal
    [0, 5, 10, 15, 20], [1, 6, 11, 16, 21], [2, 7, 12, 17, 22], [3, 8, 13, 18, 23], [4, 9, 14, 19, 24], // Vertical
    [0, 6, 12, 18, 24], [4, 8, 12, 16, 20] // Diagonal
];

// --- BALANCE PERSISTENCE ---
const BALANCE_FILE = path.join(__dirname, 'balances.json');
const DEFAULT_BALANCES = { GC: 10000, SC: 50 };

function loadBalances() {
    try {
        if (fs.existsSync(BALANCE_FILE)) {
            return JSON.parse(fs.readFileSync(BALANCE_FILE, 'utf8'));
        }
    } catch (e) {
        console.error('[WARN] Failed to load balances, using defaults:', e.message);
    }
    return { ...DEFAULT_BALANCES };
}

function saveBalances() {
    try {
        fs.writeFileSync(BALANCE_FILE, JSON.stringify(balances), 'utf8');
    } catch (e) {
        console.error('[WARN] Failed to persist balances:', e.message);
    }
}

let balances = loadBalances();

// --- PROVABLY FAIR ENGINE ---

function generateProvablyFairGrid(serverSeed, clientSeed, nonce) {
    const combinedData = `${serverSeed}:${clientSeed}:${nonce}`;
    const hash = crypto.createHash('sha256').update(combinedData).digest('hex');

    let grid = [];
    // Generate 25 symbols based on the hash string
    for (let i = 0; i < 25; i++) {
        // Take 2 characters from the hash, convert to hex integer (0 to 255)
        const hexSlice = hash.substring((i % 32) * 2, (i % 32) * 2 + 2);
        const decimalValue = parseInt(hexSlice, 16);

        // PATCH: 10-Symbol Volatility Weights (256 total values distributed)
        let selectedSymbol = "Wire"; // Default fallback

        if (decimalValue <= 44) {
            selectedSymbol = "Wire";       // ~17.5% chance
        } else if (decimalValue <= 89) {
            selectedSymbol = "Cable";      // ~17.5% chance
        } else if (decimalValue <= 129) {
            selectedSymbol = "Fan";        // ~15.6% chance
        } else if (decimalValue <= 164) {
            selectedSymbol = "Drive";      // ~13.6% chance
        } else if (decimalValue <= 194) {
            selectedSymbol = "Battery";    // ~11.7% chance
        } else if (decimalValue <= 214) {
            selectedSymbol = "RAM";        // ~7.8% chance
        } else if (decimalValue <= 226) {
            selectedSymbol = "Microchip";  // ~4.7% chance
        } else if (decimalValue <= 234) {
            selectedSymbol = "Skull";      // ~3.1% chance
        } else if (decimalValue <= 242) {
            selectedSymbol = "Neon";       // ~3.1% chance (Wild)
        } else {
            selectedSymbol = "Seven";      // ~5.0% chance (Scatter)
        }

        grid.push(selectedSymbol);
    }

    return { grid, hash };
}

// --- WIN EVALUATION ENGINE (PAYLINES + SCATTER TRIGGER) ---

function calculateWin(gridArray, betAmount) {
    let totalWin = 0;
    let scatterCount = 0;

    // 1. Count Scatters anywhere on the board
    gridArray.forEach(symbol => {
        if (symbol === "Seven") scatterCount++;
    });

    console.log(`[EVALUATION] Scatters detected: ${scatterCount}`);

    // 2. Evaluate all predefined Lines (Strict Left-to-Right)
    LINES.forEach((line, lineIndex) => {
        const lineSymbols = line.map(index => gridArray[index]);
        let highestLineWin = 0;

        let currentStreak = 0;
        let targetSymbol = null;

        for (let j = 0; j < 5; j++) {
            const sym = lineSymbols[j];
            if (sym === "Seven") break; // Scatters do not form line wins

            // First non-wild symbol sets the target for this streak
            if (targetSymbol === null && sym !== "Neon") {
                targetSymbol = sym;
            }

            // Streak continues if it matches the target, or is a Wild, or if we are starting with Wilds
            if (sym === targetSymbol || sym === "Neon" || targetSymbol === null) {
                currentStreak++;
            } else {
                break; // Left-to-right chain broken
            }
        }

        // If the entire streak is Wilds ("Neon"), pay as the highest symbol ("Skull")
        const symbolToPay = targetSymbol || "Skull";

        // Check against paytable
        if (currentStreak >= 3 && PAYTABLE[symbolToPay] && PAYTABLE[symbolToPay][currentStreak]) {
            highestLineWin = Math.floor(betAmount * PAYTABLE[symbolToPay][currentStreak]);
        }

        if (highestLineWin > 0) {
            totalWin += highestLineWin;
            console.log(`[WIN DETECTED] Line pays +${highestLineWin}`);
        }
    });

    // 3. Scatter Bonus Trigger Payout
    if (scatterCount >= 5) {
        const scatterPayout = betAmount * 10;
        totalWin += scatterPayout;
        console.log(`[SCATTER HIT] 5+ Sevens = Bonus Exploit Triggered (+${scatterPayout})`);
    }

    return totalWin;
}

// --- PROVABLY FAIR: Pre-committed server seed for next spin ---
// Server reveals its seed after the spin so the client can verify the outcome.
// The commitment (hash of server seed) is sent before the spin.
let pendingServerSeed = crypto.randomBytes(32).toString('hex');

function getServerSeedCommitment() {
    return crypto.createHash('sha256').update(pendingServerSeed).digest('hex');
}

// --- INPUT VALIDATION ---
const VALID_CURRENCIES = new Set(['GC', 'SC']);
const BET_LIMITS = { GC: { min: 1, max: 5000 }, SC: { min: 1, max: 25 } };

function validateSpinInput(currencyType, betAmount) {
    if (!VALID_CURRENCIES.has(currencyType)) {
        return 'INVALID_CURRENCY';
    }
    if (typeof betAmount !== 'number' || !Number.isInteger(betAmount) || betAmount < BET_LIMITS[currencyType].min || betAmount > BET_LIMITS[currencyType].max) {
        return 'INVALID_BET';
    }
    return null;
}

// --- API ENDPOINTS ---

app.get('/api/init', (req, res) => {
    const serverSeed = crypto.randomBytes(32).toString('hex');
    const nonce = crypto.randomBytes(4).readUInt32BE(0);
    const { grid } = generateProvablyFairGrid(serverSeed, "init_boot_sequence", nonce);

    res.json({
        success: true,
        gridData: grid.join(','),
        balances: balances,
        nextServerSeedHash: getServerSeedCommitment()
    });
    console.log(`[SYSTEM] Initial board state sent to client.`);
});

app.post('/api/spin', (req, res) => {
    const { currencyType, betAmount, clientSeed } = req.body;

    const validationError = validateSpinInput(currencyType, betAmount);
    if (validationError) {
        return res.status(400).json({ success: false, error: validationError });
    }

    if (!clientSeed || typeof clientSeed !== 'string' || clientSeed.length < 8 || clientSeed.length > 128) {
        return res.status(400).json({ success: false, error: 'INVALID_CLIENT_SEED' });
    }

    if (balances[currencyType] < betAmount) {
        return res.status(402).json({ success: false, error: 'INSUFFICIENT_FUNDS' });
    }

    // Use and rotate the pre-committed server seed
    const serverSeed = pendingServerSeed;
    pendingServerSeed = crypto.randomBytes(32).toString('hex');

    balances[currencyType] -= betAmount;

    const nonce = crypto.randomBytes(4).readUInt32BE(0);
    const { grid, hash } = generateProvablyFairGrid(serverSeed, clientSeed, nonce);

    const winAmount = calculateWin(grid, betAmount);
    balances[currencyType] += winAmount;
    saveBalances();

    res.json({
        success: true,
        currencyType: currencyType,
        betAmount: betAmount,
        winAmount: winAmount,
        newBalance: balances[currencyType],
        gridData: grid.join(','),
        provablyFair: { hash, serverSeed, clientSeed, nonce },
        nextServerSeedHash: getServerSeedCommitment()
    });

    console.log(`[TRANSACTION] Bet: ${betAmount} | Won: ${winAmount} | New Balance: ${balances[currencyType]} ${currencyType}`);
});

const PORT = 3000;
app.listen(PORT, () => {
    console.log(`[SYSTEM] Zero-Day Node.js Server initialized on port ${PORT}`);
    console.log(`[SYSTEM] Awaiting WebGL Sweepstakes connections...`);
});