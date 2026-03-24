const express = require('express');
const cors = require('cors');
const crypto = require('crypto');

const app = express();
const PORT = process.env.PORT || 3000;

// Middleware to parse JSON and allow Cross-Origin requests from our Unity frontend
app.use(cors());
app.use(express.json());

/**
 * MOCK DATABASE
 * In a production Web3 environment, this would be tied to a MongoDB database
 * and verified via ethers.js/smart contracts. 
 * GC = Gold Coins (Play for fun)
 * SC = Sweeps Coins (Promotional, redeemable for crypto)
 */
let playerProfile = {
    username: "PlayerOne",
    gcBalance: 10000,
    scBalance: 5.00,
    nonce: 0 // Tracks total spins to ensure Provably Fair sequence integrity
};

/**
 * PROVABLY FAIR RNG ALGORITHM
 * Industry-standard HMAC-SHA256 implementation.
 */
function generateProvablyFairOutcome(serverSeed, clientSeed, nonce) {
    // Combine the client seed and nonce to create the message
    const message = `${clientSeed}:${nonce}`;
    
    // Create an HMAC-SHA256 hash using the server seed as the secret key
    const hmac = crypto.createHmac('sha256', serverSeed);
    hmac.update(message);
    const hash = hmac.digest('hex');

    // Convert the first 8 characters of the hex hash into an integer
    const subHash = hash.substring(0, 8);
    const decimalValue = parseInt(subHash, 16);

    // Generate a number between 0 and 99
    const roll = decimalValue % 100;
    
    return { hash, roll };
}

// --- API ENDPOINTS ---

// 1. Fetch Player Profile
app.get('/api/profile', (req, res) => {
    res.json({ success: true, profile: playerProfile });
});

// 2. Execute Spin Logic
app.post('/api/spin', (req, res) => {
    const { currencyType, betAmount, clientSeed } = req.body;

    // Validation: Check if inputs are valid
    if (!currencyType || (currencyType !== 'GC' && currencyType !== 'SC') || betAmount <= 0) {
        return res.status(400).json({ success: false, error: "Invalid bet parameters." });
    }

    // Validation: Check if player has enough funds
    if (currencyType === 'GC' && playerProfile.gcBalance < betAmount) {
        return res.status(400).json({ success: false, error: "Insufficient Gold Coins." });
    }
    if (currencyType === 'SC' && playerProfile.scBalance < betAmount) {
        return res.status(400).json({ success: false, error: "Insufficient Sweeps Coins." });
    }

    // Deduct the bet
    if (currencyType === 'GC') playerProfile.gcBalance -= betAmount;
    if (currencyType === 'SC') playerProfile.scBalance -= betAmount;

    // Generate Provably Fair Seeds (In production, the server seed is generated BEFORE the bet)
    const serverSeed = crypto.randomBytes(32).toString('hex');
    const actualClientSeed = clientSeed || "default_player_seed";
    
    // Execute the roll
    const { hash, roll } = generateProvablyFairOutcome(serverSeed, actualClientSeed, playerProfile.nonce);
    
    // Determine the win (Simple logic: 40% chance to win 2x the bet)
    let winAmount = 0;
    if (roll < 40) {
        winAmount = betAmount * 2;
        if (currencyType === 'GC') playerProfile.gcBalance += winAmount;
        if (currencyType === 'SC') playerProfile.scBalance += winAmount;
    }

    // Increment nonce for the next spin
    playerProfile.nonce += 1;

    // Send the secure payload back to the client
    res.json({
        success: true,
        rollResult: roll,
        winAmount: winAmount,
        currencyType: currencyType,
        newBalance: currencyType === 'GC' ? playerProfile.gcBalance : playerProfile.scBalance,
        provablyFair: {
            hash: hash,
            serverSeed: serverSeed,
            clientSeed: actualClientSeed,
            nonce: playerProfile.nonce - 1
        }
    });
});

// Start the server
app.listen(PORT, () => {
    console.log(`[SYSTEM] Provably Fair Sweepstakes Engine running on http://localhost:${PORT}`);
    console.log(`[SYSTEM] Awaiting Web3 Handshake...`);
});