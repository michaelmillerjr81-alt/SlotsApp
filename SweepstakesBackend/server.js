const express = require('express');
const cors = require('cors');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const jwt = require('jsonwebtoken');
const bcrypt = require('bcryptjs');

const app = express();
app.use(cors());
app.use(express.json());

// ---- ENV VALIDATION ----
const API_KEY = process.env.API_KEY;
const JWT_SECRET = process.env.JWT_SECRET;
const PORT = process.env.PORT || 3000;

if (!API_KEY || !JWT_SECRET) {
    console.error('[FATAL] API_KEY and JWT_SECRET environment variables are required.');
    process.exit(1);
}

// ---- API KEY MIDDLEWARE (all routes) ----
app.use((req, res, next) => {
    const key = req.headers['x-api-key'];
    if (!key || key !== API_KEY) {
        return res.status(401).json({ success: false, error: 'UNAUTHORIZED' });
    }
    next();
});

// ---- JWT MIDDLEWARE (factory) ----
function requireAuth(req, res, next) {
    const authHeader = req.headers['authorization'];
    const token = authHeader && authHeader.startsWith('Bearer ') ? authHeader.slice(7) : null;
    if (!token) return res.status(401).json({ success: false, error: 'UNAUTHORIZED' });
    try {
        req.user = jwt.verify(token, JWT_SECRET);
        next();
    } catch {
        return res.status(401).json({ success: false, error: 'TOKEN_EXPIRED' });
    }
}

// ---- USER DATABASE (JSON file) ----
const USERS_FILE = path.join(__dirname, 'users.json');

function loadUsers() {
    try {
        if (fs.existsSync(USERS_FILE)) {
            return JSON.parse(fs.readFileSync(USERS_FILE, 'utf8'));
        }
    } catch (e) {
        console.error('[WARN] Failed to load users DB:', e.message);
    }
    return {};
}

function saveUsers(users) {
    try {
        fs.writeFileSync(USERS_FILE, JSON.stringify(users, null, 2), 'utf8');
    } catch (e) {
        console.error('[WARN] Failed to save users DB:', e.message);
    }
}

// ---- GAME REGISTRY ----
const GAMES = [
    {
        id: 'zero-day-slots',
        name: 'Zero Day Slots',
        sceneName: 'ZeroDaySlots',
        description: '5x5 cyberpunk slots. 12 paylines, wilds, and scatter bonus.',
        minBet: { GC: 100, SC: 5 },
        maxBet: { GC: 10000, SC: 500 }
    }
];

// ---- CASINO CONFIGURATION ----
const SYMBOLS = ["Wire", "Cable", "Fan", "Drive", "Battery", "RAM", "Microchip", "Skull", "Neon", "Seven"];

const PAYTABLE = {
    "Wire":      { 3: 0.1,  4: 0.2,  5: 0.5  },
    "Cable":     { 3: 0.1,  4: 0.3,  5: 1.0  },
    "Fan":       { 3: 0.2,  4: 0.4,  5: 1.2  },
    "Drive":     { 3: 0.2,  4: 0.5,  5: 1.5  },
    "Battery":   { 3: 0.5,  4: 1.0,  5: 2.0  },
    "RAM":       { 3: 0.8,  4: 1.5,  5: 3.0  },
    "Microchip": { 3: 1.0,  4: 2.5,  5: 5.0  },
    "Skull":     { 3: 2.0,  4: 5.0,  5: 15.0 },
};

const LINES = [
    [0,1,2,3,4], [5,6,7,8,9], [10,11,12,13,14], [15,16,17,18,19], [20,21,22,23,24],
    [0,5,10,15,20], [1,6,11,16,21], [2,7,12,17,22], [3,8,13,18,23], [4,9,14,19,24],
    [0,6,12,18,24], [4,8,12,16,20]
];

const BET_LIMITS = { GC: { min: 100, max: 10000 }, SC: { min: 5, max: 500 } };
const VALID_CURRENCIES = new Set(['GC', 'SC']);

// ---- PROVABLY FAIR ENGINE ----
function generateProvablyFairGrid(serverSeed, clientSeed, nonce) {
    const combinedData = `${serverSeed}:${clientSeed}:${nonce}`;
    const hash = crypto.createHash('sha256').update(combinedData).digest('hex');
    let grid = [];
    for (let i = 0; i < 25; i++) {
        const hexSlice = hash.substring((i % 32) * 2, (i % 32) * 2 + 2);
        const v = parseInt(hexSlice, 16);
        let sym = "Wire";
        if      (v <= 44)  sym = "Wire";
        else if (v <= 89)  sym = "Cable";
        else if (v <= 129) sym = "Fan";
        else if (v <= 164) sym = "Drive";
        else if (v <= 194) sym = "Battery";
        else if (v <= 214) sym = "RAM";
        else if (v <= 226) sym = "Microchip";
        else if (v <= 234) sym = "Skull";
        else if (v <= 242) sym = "Neon";
        else               sym = "Seven";
        grid.push(sym);
    }
    return { grid, hash };
}

// ---- WIN EVALUATION ----
function calculateWin(gridArray, betAmount) {
    let totalWin = 0;
    let scatterCount = gridArray.filter(s => s === "Seven").length;

    LINES.forEach(line => {
        const lineSymbols = line.map(i => gridArray[i]);
        let streak = 0;
        let target = null;

        for (let j = 0; j < 5; j++) {
            const sym = lineSymbols[j];
            if (sym === "Seven") break;
            if (target === null && sym !== "Neon") target = sym;
            if (sym === target || sym === "Neon" || target === null) streak++;
            else break;
        }

        const symbolToPay = target || "Skull";
        if (streak >= 3 && PAYTABLE[symbolToPay]?.[streak]) {
            totalWin += Math.floor(betAmount * PAYTABLE[symbolToPay][streak]);
        }
    });

    if (scatterCount >= 5) {
        totalWin += betAmount * 10;
    }

    return totalWin;
}

// ---- AUTH ROUTES ----

// POST /api/auth/register
app.post('/api/auth/register', async (req, res) => {
    const { username, password } = req.body;

    if (!username || typeof username !== 'string' || !/^[a-zA-Z0-9_]{3,20}$/.test(username)) {
        return res.status(400).json({ success: false, error: 'INVALID_USERNAME' });
    }
    if (!password || typeof password !== 'string' || password.length < 8) {
        return res.status(400).json({ success: false, error: 'WEAK_PASSWORD' });
    }

    const users = loadUsers();
    if (users[username.toLowerCase()]) {
        return res.status(409).json({ success: false, error: 'USERNAME_TAKEN' });
    }

    const passwordHash = await bcrypt.hash(password, 10);
    const userKey = username.toLowerCase();
    users[userKey] = {
        username,
        passwordHash,
        gcBalance: 10000,
        scBalance: 50,
        pendingServerSeed: crypto.randomBytes(32).toString('hex'),
        createdAt: Date.now()
    };
    saveUsers(users);

    const token = jwt.sign({ username: userKey }, JWT_SECRET, { expiresIn: '24h' });
    console.log(`[AUTH] New account registered: ${username}`);

    res.json({
        success: true,
        token,
        username: users[userKey].username,
        gcBalance: users[userKey].gcBalance,
        scBalance: users[userKey].scBalance
    });
});

// POST /api/auth/login
app.post('/api/auth/login', async (req, res) => {
    const { username, password } = req.body;

    if (!username || !password) {
        return res.status(400).json({ success: false, error: 'MISSING_CREDENTIALS' });
    }

    const users = loadUsers();
    const userKey = username.toLowerCase();
    const user = users[userKey];

    if (!user) {
        return res.status(401).json({ success: false, error: 'INVALID_CREDENTIALS' });
    }

    const valid = await bcrypt.compare(password, user.passwordHash);
    if (!valid) {
        return res.status(401).json({ success: false, error: 'INVALID_CREDENTIALS' });
    }

    const token = jwt.sign({ username: userKey }, JWT_SECRET, { expiresIn: '24h' });
    console.log(`[AUTH] Login: ${user.username}`);

    res.json({
        success: true,
        token,
        username: user.username,
        gcBalance: user.gcBalance,
        scBalance: user.scBalance
    });
});

// ---- PLATFORM ROUTES ----

// GET /api/games
app.get('/api/games', requireAuth, (req, res) => {
    res.json({ success: true, games: GAMES });
});

// GET /api/player
app.get('/api/player', requireAuth, (req, res) => {
    const users = loadUsers();
    const user = users[req.user.username];
    if (!user) return res.status(404).json({ success: false, error: 'USER_NOT_FOUND' });

    res.json({
        success: true,
        username: user.username,
        gcBalance: user.gcBalance,
        scBalance: user.scBalance
    });
});

// ---- GAME ROUTES ----

// GET /api/init
app.get('/api/init', requireAuth, (req, res) => {
    const users = loadUsers();
    const user = users[req.user.username];
    if (!user) return res.status(404).json({ success: false, error: 'USER_NOT_FOUND' });

    const serverSeed = crypto.randomBytes(32).toString('hex');
    const nonce = crypto.randomBytes(4).readUInt32BE(0);
    const { grid } = generateProvablyFairGrid(serverSeed, "init_boot_sequence", nonce);

    const commitment = crypto.createHash('sha256').update(user.pendingServerSeed).digest('hex');

    res.json({
        success: true,
        gridData: grid.join(','),
        balances: { GC: user.gcBalance, SC: user.scBalance },
        nextServerSeedHash: commitment
    });
});

// POST /api/spin
app.post('/api/spin', requireAuth, (req, res) => {
    const { currencyType, betAmount, clientSeed } = req.body;

    if (!VALID_CURRENCIES.has(currencyType)) {
        return res.status(400).json({ success: false, error: 'INVALID_CURRENCY' });
    }
    if (typeof betAmount !== 'number' || !Number.isInteger(betAmount) ||
        betAmount < BET_LIMITS[currencyType].min || betAmount > BET_LIMITS[currencyType].max) {
        return res.status(400).json({ success: false, error: 'INVALID_BET' });
    }
    if (!clientSeed || typeof clientSeed !== 'string' || clientSeed.length < 8 || clientSeed.length > 128) {
        return res.status(400).json({ success: false, error: 'INVALID_CLIENT_SEED' });
    }

    const users = loadUsers();
    const user = users[req.user.username];
    if (!user) return res.status(404).json({ success: false, error: 'USER_NOT_FOUND' });

    const balanceKey = currencyType === 'GC' ? 'gcBalance' : 'scBalance';
    if (user[balanceKey] < betAmount) {
        return res.status(402).json({ success: false, error: 'INSUFFICIENT_FUNDS' });
    }

    // Use and rotate the pre-committed server seed
    const serverSeed = user.pendingServerSeed;
    user.pendingServerSeed = crypto.randomBytes(32).toString('hex');

    user[balanceKey] -= betAmount;

    const nonce = crypto.randomBytes(4).readUInt32BE(0);
    const { grid, hash } = generateProvablyFairGrid(serverSeed, clientSeed, nonce);

    const winAmount = calculateWin(grid, betAmount);
    user[balanceKey] += winAmount;

    saveUsers(users);

    const commitment = crypto.createHash('sha256').update(user.pendingServerSeed).digest('hex');

    console.log(`[SPIN] ${user.username} | Bet: ${betAmount} ${currencyType} | Won: ${winAmount} | Balance: ${user[balanceKey]}`);

    res.json({
        success: true,
        currencyType,
        betAmount,
        winAmount,
        newBalance: user[balanceKey],
        gridData: grid.join(','),
        provablyFair: { hash, serverSeed, clientSeed, nonce },
        nextServerSeedHash: commitment
    });
});

// ---- START ----
app.listen(PORT, () => {
    console.log(`[SYSTEM] ZeroDay Platform API running on port ${PORT}`);
});
