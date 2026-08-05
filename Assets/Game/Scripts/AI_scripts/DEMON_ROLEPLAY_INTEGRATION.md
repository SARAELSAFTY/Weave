# AI Demon Roleplay Integration Guide
## Groq-Powered Multi-Turn Chat with Belief-Based Consequences

### Table of Contents
1. [Overview](#overview)
2. [Workflow & UX Flow](#workflow--ux-flow)
3. [Core Architecture](#core-architecture)
4. [Data Structures](#data-structures)
5. [API Integration](#api-integration)
6. [System Design](#system-design)
7. [Belief Mechanics](#belief-mechanics)
8. [Error Handling & Fallback](#error-handling--fallback)
9. [Testing & Tuning](#testing--tuning)

---

## Overview

**Core Concept:** When a player makes a card choice, a demon overlay appears and engages in **multi-turn dialogue**. The player can challenge or ask follow-up questions (1-4 exchanges). Whether the player *believes* the demon determines actual game consequences.

**Key Difference from Static Commentary:**
- ❌ OLD: Demon speaks once, consequence applies regardless
- ✅ NEW: Player engages with demon, belief meter rises/falls, consequence depends on final belief state

**Belief System:**
- Demon tells truth/lie based on deception mode
- Player evaluates demon's claims through multi-turn chat
- **If player TRUSTS**: Consequence follows player's belief
- **If player DISTRUSTS**: Consequence follows game truth

**Example:**
```
Card Choice: "Raise taxes" (Wealth +10, Stability -5)
Demon Claims: "Your people will revolt! Chaos ensues!" (LIE)

Player Response 1: "But my treasury is empty..."
Demon Response: "Exactly! Taxing peasants is suicide. They will burn your palace."

Player Response 2: "You're just trying to scare me."
Demon Response: "Believe as you wish, fool. The choice is yours."

Final Outcome:
- If player BELIEVED demon: No tax increase (missed +10 wealth)
- If player DISTRUSTED demon: Tax increase happens (+10 wealth, -5 stability)
```

---

## Workflow & UX Flow

### Phase 1: Card Appears
```
GameManager → NarrativeRunner loads Card_004
↓
Card appears with:
  - Card description/narrative
  - Left choice button
  - Right choice button
```

### Phase 2: Player Chooses
```
Player taps LEFT or RIGHT button
↓
Card records choice (stored in temp variable)
Choice buttons DISABLED (cannot change mind)
↓
→ Move to Phase 3
```

### Phase 3: Demon Overlay Appears
```
DemonUIManager spawns overlay on top of card
↓
Overlay shows:
  - Demon portrait (sinister)
  - Demon's initial commentary (1-2 sentences)
  - Chat input field (text box for player response)
  - Character count indicator
↓
Groq API called with:
  - Card description
  - Player's chosen path
  - Deception mode (TRUTH/LIE/WARNING/CONFUSE)
  - Conversation history (empty on first exchange)
```

### Phase 4: Multi-Turn Dialogue (1-4 Exchanges)
```
EXCHANGE 1:
  Player reads demon's initial message
  Player types follow-up question or statement
  ↓
  DemonChatManager → GroqService.GetDemonResponse()
  ↓
  Demon responds with new message
  Belief meter updates based on:
    - Length of exchange
    - Contradiction detection
    - Confidence language analysis

EXCHANGE 2-4 (Optional):
  Player can respond again (up to 4 total exchanges)
  Each exchange:
    - Adds to conversation history
    - Calls Groq API with full context
    - Updates belief meter
    - Checks if demon/player willing to end chat
  ↓
  Or player swipes to dismiss
```

### Phase 5: Demon Overlay Closes
```
Player SWIPES (left or right)
↓
Overlay fades out
Card remains visible
↓
→ Move to Phase 6
```

### Phase 6: Consequences Apply (Based on Belief)
```
FinalBeliefScore calculated:
  - Initial deception mode accuracy
  - Each exchange credibility check
  - Player's final belief state (-100 to +100)

IF beliefScore > 0 (Player TRUSTED demon):
  Apply DEMON'S CLAIMED consequences
  (May be false if demon lied)
ELSE (Player DISTRUSTED demon):
  Apply TRUE consequences
  (What actually happens)

Examples:
  Demon claimed: Wealth -10 (LIE, actually +5)
  Player belief: +75 (strong trust)
  → Result: Wealth -10 (believed demon, got lied to)

  Demon claimed: Wealth -10 (TRUTH)
  Player belief: -40 (distrusted)
  → Result: Wealth +5 (didn't trust demon, got true outcome)
```

### Phase 7: Card Disappears
```
Consequences animate/display
↓
Card slides out
↓
Next eligible card loads
→ Return to Phase 1
```

---

## Core Architecture

### File Structure

**Core Systems:**
- GroqService: Handles all API calls to Groq with retry logic and timeout management
- DemonPersonaManager: Selects deception mode and generates claimed consequences
- DemonChatManager: Orchestrates multi-turn conversation, belief tracking, and session state
- CardLevelDetector: Checks card eligibility based on player progression level
- AISystemManager: Central singleton coordinating all AI systems

**UI Layer:**
- DemonOverlayUI: Popup overlay with demon portrait and styling
- DemonChatPanel: Chat display window, message formatting, input field
- BeliefMeterUI: Optional visual indicator of trust level (visualizes -100 to +100 score)

**Data & Config:**
- DemonConfig: Tunable parameters (truth ratios, conversation limits, behavior modifiers)
- CardLevelDefinitions: Level requirements and unlock conditions
- ConversationTurn: Stores individual message with speaker, content, trust delta
- DeceptionModes: Enum defining TRUTH / LIE / WARNING / CONFUSE modes

**Prompts:**
- demon_system_prompt.txt: Template system prompt injected into every Groq request

### System Flow Diagram

```
GameManager receives player choice
        ↓
Card records choice (LEFT or RIGHT)
        ↓
Disable choice buttons (cannot change mind)
        ↓
Check: Is this a demon card? OR should demon appear?
        ↓
YES → Spawn DemonOverlay
        ↓
DemonPersonaManager selects deception mode (TRUTH/LIE/WARNING/CONFUSE)
        ↓
GroqService called → Get initial demon message
        ↓
DemonChatPanel displays message + input field
        ↓
        ┌─────────────────────────────────────┐
        │ MULTI-TURN CONVERSATION LOOP        │
        │ (1-4 exchanges)                     │
        │                                     │
        │ Player types response               │
        │      ↓                              │
        │ Calculate trust delta               │
        │      ↓                              │
        │ Update belief score                 │
        │      ↓                              │
        │ GroqService called with history     │
        │      ↓                              │
        │ Display demon response              │
        │      ↓                              │
        │ Check: Max exchanges reached?       │
        │ Or player swiped to dismiss?        │
        └─────────────────────────────────────┘
        ↓
DemonOverlay closes (fade out)
        ↓
ConsequenceSystem evaluates final belief score:
   IF belief > 0: Apply demon's CLAIMED consequences
   IF belief < 0: Apply TRUE consequences
        ↓
Card animates out
        ↓
Next eligible card loads
```

---

## Data Structures

### 1. Extended CardData

Each card needs additional fields beyond the existing card description:

**Level Gating Fields:**
- levelRequired (int): Minimum player level to unlock this card
- levelCategory (string): "early", "mid", "late", or "endgame" for organizational purposes

**Demon Integration:**
- isDemonCard (bool): Marks cards that explicitly feature demon interaction
- demonInitialTone (string): Sets the tone for demon's first message ("seductive", "mocking", "cryptic", "threatening")

**Consequence Data:**
- Array of consequences mapping each resource (wealth, stability, military, etc.) to values for left vs. right choice
- Each consequence stores: resource name, value if left chosen, value if right chosen

**Editor Hint:**
- expectedDemonMode: Suggestion for what deception mode fits this card (not enforced, helps designers understand the flow)

### 2. ConversationTurn (Chat History)

Represents a single message exchange in the demon conversation.

**Fields:**
- speaker: Identifies who sent the message (PLAYER or DEMON)
- message: The actual text content
- timestamp: When the message was sent (for logging)
- trustDelta: How much this exchange changed belief (-10 to +10 range)
- beliefReason: Why trust changed (for player feedback or analytics)

Stored in order as a conversation history that gets passed to Groq for context.

### 3. DemonChatSession

Tracks the entire demon conversation for one card choice.

**Session Metadata:**
- sessionId: Unique identifier for this conversation
- currentCard: Reference to the card being played
- playerChosenSide: "LEFT" or "RIGHT" (the initial choice that triggered demon)

**Conversation State:**
- conversationHistory: List of all ConversationTurn objects in order
- exchangeCount: How many turns have occurred (capped at MAX_EXCHANGES = 4)
- demonSpeaking: Boolean indicating whose turn it is

**Belief Tracking:**
- beliefScore: Range -100 to +100, updated after each exchange
- demonMode: The deception mode selected for this session (TRUTH/LIE/WARNING/CONFUSE)
- demonClaimedConsequences: What demon says will happen
- trueConsequences: What actually happens (secret from player unless they disbelieve)

**Timing:**
- sessionStartTime: When the conversation began (for analytics, potential timeout)

### 4. DemonConfig (ScriptableObject)

Central configuration object for all demon behavior tuning.

**Deception Ratios:**
- truthRatio (0-100): % chance demon tells truth
- lieRatio (0-100): % chance demon lies
- warningRatio (0-100): % chance demon warns falsely
- confuseRatio (0-100): % chance demon confuses

These add together to 100 and determine which mode is selected.

**Conversation Behavior:**
- minExchangesBeforeDismiss: Minimum exchanges before demon allows player to leave (prevents cheese)
- maxExchanges: Hard cap at 4 exchanges
- characterPerTurnLimit: 500 character limit per player message
- demonTypingDelay: Delay between exchanges in seconds (pacing)

**Belief Dynamics:**
- beliefChangePerExchange: Base trust shift per turn (e.g., 15 points)
- beliefBonusForDemonContradiction: Extra skepticism if player detects inconsistency (e.g., +25)
- beliefPenaltyForTruthfulDemon: Negative bonus if demon is being truthful (seems counterintuitive but represents player's paranoia)

**Appearance:**
- demonPortrait: Sprite image for overlay
- demonName: "The Whisper", "The Deceiver", configurable
- demonTitle: "Dark Counsel" or similar

**API Settings:**
- groqApiKey: Set in inspector (never commit to repo)
- groqModel: "mixtral-8x7b-32768" or "llama-2-70b-chat"
- maxTokensPerResponse: Budget for Groq output (150 tokens for short responses)
- apiTimeoutSeconds: Maximum wait before giving up (5 seconds)

### 5. DeceptionMode Enum

Defines four possible modes for demon behavior in any conversation.

**TRUTH:** Demon tells the actual consequence but frames it seductively or ominously. Player hears reality but twisted through demonic lens.

**LIE:** Demon inverts or distorts the consequence. Promises false gains or exaggerates false dangers. Most deceptive mode.

**WARNING:** Demon warns of terrible consequences that don't actually exist. Induces paranoia and fear about fake catastrophes.

**CONFUSE:** Demon speaks in riddles and half-truths. Leaves player genuinely uncertain what they mean. Theatrical and mysterious.

### 6. GameConsequence

Represents a single resource change resulting from a card choice.

**Fields:**
- resourceType: Name of resource ("wealth", "stability", "military", etc.)
- trueValue: What actually happens (secret unless player distrusts demon)
- claimedValue: What demon claims will happen (false if demon lied)
- demonToldTruth: Flag indicating if this claim was truthful

---

## API Integration

### Groq API Architecture

**Endpoint:** https://api.groq.com/openai/v1/chat/completions

**Model Selection:**
- Primary: mixtral-8x7b-32768 (fast, capable, ideal for quick responses)
- Alternative: llama-2-70b-chat (longer context if needed)

**Request Structure:**
Each API call to Groq follows the standard OpenAI chat format with three components:
1. System message (injected each time with full context)
2. Conversation history (all previous exchanges in this session)
3. Current user message (player's latest input)

### System Prompt Strategy

A single template system prompt is injected into every Groq request. The prompt includes:

**Character Definition:**
- Establishes demon identity (configurable name, title)
- Explains the demon knows actual consequences but chooses to distort them
- Sets tone guidance (seductive, mocking, mysterious, threatening)

**Deception Mode Instructions:**
For each session, the system prompt includes explicit instructions for the assigned deception mode:
- TRUTH: Describe actual consequence but frame seductively/ominously
- LIE: Invert or distort the consequence, promise false gains or exaggerate dangers
- WARNING: Warn of terrible consequences that don't actually exist
- CONFUSE: Speak in riddles and half-truths, leave player uncertain

**Conversation Rules:**
- Stay in character—never break immersion
- Respond directly to player's statements or questions
- Keep responses 1-3 sentences (brief, punchy)
- Remember previous turns (history is provided)
- Defend position if challenged (in character)
- After 4th exchange, be willing to let player dismiss

**Scenario Context:**
- Card description (what choice was made)
- Player's chosen path (LEFT or RIGHT)
- True consequence (what actually happens)
- Claimed consequence (what demon says happens)

### Call Flow: Initial Message

When a card choice triggers demon interaction:

1. DemonPersonaManager rolls deception mode (TRUTH/LIE/WARNING/CONFUSE)
2. Generate claimed consequences (what demon will claim to the player)
3. Build system prompt with all context injected
4. Make API request to Groq with system + initial user message
5. Groq returns demon's initial commentary
6. Display in overlay with portrait

**Expected latency:** <1 second (Groq is fast)

### Call Flow: Multi-Turn Responses

For each subsequent player message (turns 2-4):

1. Retrieve full conversation history from DemonChatSession
2. Build system prompt (same template, same deception mode)
3. Append all previous exchanges in order (player → demon → player → demon...)
4. Append current player message
5. Make API request to Groq with full history
6. Groq returns demon's response with full context
7. Calculate trust delta based on:
   - Contradiction detection (player challenging claims)
   - Confidence language analysis (how convincing was response)
   - Length of response (longer = more convincing, to a point)
   - Hedge language detection (reduces trust)
8. Update belief score
9. Display response in chat panel
10. Check if this was the 4th exchange or player swiped

### Token Budget

Per-call estimate:
- System prompt: ~150 tokens
- Conversation history (4 exchanges max): ~200-300 tokens
- Current player message: ~50-100 tokens
- Response buffer: ~150 tokens
- **Total per call: ~500-700 tokens** (well within Groq limits)

### Timeout & Fallback Strategy

If Groq API fails or times out after 5 seconds:

1. Log error for debugging
2. Do NOT crash or hang
3. Retrieve a random fallback message from predefined library
4. Continue conversation as if Groq responded
5. Use deterministic (non-API) trust calculation for remaining exchanges
6. Player doesn't know API failed—gameplay continues smoothly

**Fallback Messages by Mode:**
- TRUTH fallbacks: "The consequences are as you suspect, ruler."
- LIE fallbacks: "Your triumph is assured... or so it seems."
- WARNING fallbacks: "Beware! Ruin lurks in every shadow."
- CONFUSE fallbacks: "The Whisper speaks in riddles only the wise may solve."

### Rate Limiting

Groq has generous limits, but best practice:
- 1 API call per player message (not per keystroke)
- Implement basic response caching for identical inputs
- Consider batching in future if scaling to many concurrent players

---

## System Design

### DemonPersonaManager Responsibilities

The DemonPersonaManager selects which deception mode to use for each session based on multiple factors:

**Deception Mode Selection:**
- Rolls a random value (0-100) to determine mode
- Uses configurable ratios (truthRatio, lieRatio, warningRatio, confuseRatio)
- Applies gameplay modifiers:
  - First demon appearance: +20 bonus to truthRatio (build initial trust)
  - Late game (level >50): +30 bonus to lieRatio (demon becomes more deceptive)
  - After detected lie: -10 penalty to future truthRatio (player learning)

**Consequence Generation:**
- For each resource affected by the card choice:
  - Get TRUE consequence (stored in CardData)
  - Generate CLAIMED consequence based on deception mode:
    - TRUTH: claimed = true (play it straight)
    - LIE: claimed = inverse of true (flip the sign, add variance)
    - WARNING: claimed = exaggerated danger or catastrophe
    - CONFUSE: claimed = ambiguous (could be interpreted either way)

**Configuration Example:**
- truthRatio: 50 (50% chance demon tells truth on any card)
- lieRatio: 40 (40% chance demon lies)
- warningRatio: 50 (50% chance of false warnings)
- confuseRatio: 10 (10% chance of riddling)

These represent independent probabilities; the actual mode selection uses cumulative rolls.

### DemonChatManager Responsibilities

This is the orchestrator of the entire multi-turn conversation.

**Session Initialization:**
1. Create new DemonChatSession with unique ID
2. Store reference to current card and player's initial choice
3. Request deception mode from DemonPersonaManager
4. Generate both true and claimed consequences
5. Call GroqService for initial demon message
6. Display overlay with demon portrait

**Exchange Management (Loop for up to 4 turns):**
1. Display previous demon message in chat window
2. Enable player input field
3. Wait for player to type and submit
4. Validate input (length limits, non-empty)
5. Add player message to conversation history
6. Display player message immediately in chat
7. Calculate provisional trust delta based on language analysis
8. Show typing indicator ("The Whisper considers...")
9. Call GroqService with full conversation history
10. Add demon response to conversation history
11. Update belief score with calculated delta
12. Display demon response in chat window
13. Update visual belief meter (if used)
14. Check if exchange count >= 4 OR player dismissed → exit loop

**Belief Score Calculation:**
After each exchange, analyze the player's message and demon's response:

- Base change: ±15 points per exchange
- Contradiction detection: -25 if player uses words like "but", "that doesn't", "lie"
- Confidence language: +8 if demon uses "I know", "certainly", "trust me"
- Hedge language: -5 if demon uses "perhaps", "maybe", "might"
- Response length: +5 if >100 characters (more convincing)
- Clamp final delta to reasonable range (-15 to +25 per turn)

**Session Conclusion:**
1. Close overlay with fade animation
2. Evaluate final belief score
3. Pass to ConsequenceSystem for outcome determination
4. Save session data for analytics
5. Trigger card dismissal and next card load

### DemonChatPanel (UI) Responsibilities

Manages the visual overlay and user interaction.

**Overlay Display:**
- Fade-in animation when demon appears (0.3 seconds)
- Demon portrait displayed prominently
- Chat display window showing message history
- Input field for player responses (500 character limit)
- Character counter displaying current/max characters
- Submit button to send message
- Typing indicator animation while Groq processes

**Message Formatting:**
- Demon messages styled in red/dark color
- Player messages styled in blue/light color
- Each message prefixed with speaker name ("The Whisper:", "You:")
- Timestamps optional but logged
- Auto-scroll to bottom as messages accumulate

**Input Validation:**
- Enforce 500 character limit (configurable in DemonConfig)
- Disable submit button if field is empty
- Prevent message submission while API is processing (show loader)
- Re-enable input after response arrives

**State Management:**
- Track if input is active/disabled
- Show/hide typing indicator
- Display message hints ("Max 4 exchanges" after 3rd turn)
- Handle swipe dismissal (right swipe = close overlay)

### BeliefMeterUI (Optional Visual Feedback)

Displays trust level visually during conversation.

**Visual Representation:**
- Horizontal bar or radial meter
- Range: -100 (maximum distrust) to +100 (maximum trust)
- Color gradient: Red (distrustful) → Yellow (neutral) → Green (trusting)
- Updates after each exchange to show belief shift

**Placement:**
- Top right of overlay
- Or integrated into bottom of chat panel
- Should not distract from conversation

**Functionality:**
- Shows player their current belief state
- Helps player understand consequence implications
- Teaching tool: high trust = more vulnerable to demon's lies

### CardLevelDetector Responsibilities

Manages level-based card unlocking and progression.

**Card Eligibility Check:**
Before showing a card, verify:
1. currentPlayerLevel >= card.levelRequired?
2. Are all unlock conditions satisfied (story chapter, resource threshold, etc.)?
3. If NO: Skip to next card in sequence
4. If YES: Display card

**Player Level Progression:**
Increment level based on:
- +1 per card played
- +5 at story milestones (identified by levelCategory transitions)
- +1 per in-game day advanced
- +10 when major resource thresholds reached (wealth >100, stability >100, military >100)

**Unlock Conditions:**
Each card can specify:
- PROGRESSION_LEVEL: Minimum level required
- RESOURCE_THRESHOLD: Minimum wealth/stability/military to unlock
- STORY_CHAPTER: Must have completed story chapter X
- DEMON_INFLUENCE: Demon must have appeared N times

This allows flexible progression beyond simple leveling.

## Belief Mechanics

The belief system is the core mechanic that makes consequences meaningful and gives player agency.

### How Belief Score Works

**Range:** -100 (total distrust) to +100 (total trust)

**Starting Point:** 0 (neutral)

**After Each Exchange:**
- Player and demon interact
- System analyzes language for trust signals
- Trust delta calculated (-15 to +25 points typical)
- Added to running belief score
- Belief meter updated (if used)

### Trust Delta Calculation

After each exchange, the system analyzes:

**Contradiction Detection:**
- If player uses words like "but", "that doesn't", "lie", "wrong"
- System deducts -25 points (player caught something off)
- Demon's claimed consequences don't match player's understanding

**Confidence Language in Demon Response:**
- Uses like "I know", "certainty", "absolutely", "trust me", "guarantee"
- Add +8 points (seems authoritative)

**Hedge Language in Demon Response:**
- Uses like "perhaps", "maybe", "might", "seem", "possibly"
- Subtract -5 points (seems unsure)

**Response Length:**
- Longer response (>100 characters): +5 points (more convincing)
- Very brief response: -3 points (evasive)

**Deception Mode Modifiers:**
- TRUTH mode: If demon is being honest, trust naturally gravitates slightly negative (paranoia)
- LIE mode: If demon is lying, contradictions push trust negative
- WARNING mode: False alarms reduce trust over time
- CONFUSE mode: Uncertainty naturally reduces trust

### Consequence Application Logic

After conversation ends (swipe to dismiss or 4 exchanges max):

**Calculate Final Belief Score**

**If belief > 0 (Player TRUSTED demon):**
- Apply DEMON'S CLAIMED consequences (what demon said would happen)
- Even if the demon lied, player gets the false outcome
- **Example:** Demon claimed "Wealth -10", player believed it → Wealth goes down 10
  - But true consequence was +5, so player loses out on +5 AND loses 10 = net -15 deviation

**If belief < 0 (Player DISTRUSTED demon):**
- Apply TRUE consequences (what actually happens)
- Player ignores demon's claims and goes with gut feeling
- Reality plays out regardless of what demon said
- **Example:** Demon claimed "Wealth -10", player distrusted it → True +5 wealth applied

**If belief == 0 (Exactly neutral, rare):**
- Could split the difference, or default to true consequences
- Most likely players will swing one way or the other by turn 2-3

### Player Learning & Adaptation

**Memory System (Optional):**
- Track demon's accuracy over multiple cards
- If demon has lied 3 times, player might naturally distrust more next encounter
- Could apply meta-modifier to future belief calculations
- Teaches players to recognize demon patterns

**Trust Decay:**
- Player trust doesn't fully reset between conversations
- If demon lied badly in previous session, slight negative modifier applies next time
- Creates emergent gameplay where demon must rebuild trust

### Example Scenario Walkthrough

**Card:** "Raise taxes to fund army"
- True consequence: Wealth -5, Military +10, Stability -3
- Demon selected: LIE mode

**Demon's Claimed Consequences:**
- Wealth -20 (inverted and exaggerated)
- Military -5 (opposite of truth)
- Stability -15 (fabricated crisis)

**Turn 1 - Demon Speaks:**
"The people will REBEL if you tax them now. Civil war erupts. Your throne crumbles, fool!"

**Turn 1 - Player Response:**
"But I need a stronger military against the northern kingdom..."

**Analysis:**
- Player used "but" = -25 contradiction detection
- Belief score: -25 (player already suspicious)

**Turn 2 - Demon Response:**
"The north? Pah! Your own cities burn first. The military will MUTINY if unpaid. Choose carefully."

**Analysis:**
- Demon used "will MUTINY" (confident) = +8
- Demon used "BURN" (vivid, convincing) = +5
- But player's previous skepticism applies a slight penalty: -5
- Belief delta: +8 total
- Belief score: -25 + 8 = -17 (still distrustful)

**Turn 3 - Player Response (swipes to dismiss):**
Player thinks: "The demon is exaggerating. I'll raise the taxes."

**Final Belief: -17 (negative = distrusted)**

**Outcome:**
Since belief < 0, TRUE consequences apply:
- Wealth: -5 ✓
- Military: +10 ✓
- Stability: -3 ✓

Player made the RIGHT decision. The military strengthened, and while wealth dropped and stability wavered, overall the kingdom benefited.

---

**Alternate Scenario - If Player Had Trusted:**

**Turn 2 Alternative - Demon Response (same as before):**
"The north? Pah! Your own cities burn first. The military will MUTINY if unpaid."

**Analysis (Different Player):**
- Player didn't challenge = no contradiction
- Demon's confident tone very convincing to this player: +15
- Belief delta: +15
- Belief score: 0 + 15 = +15 (starting to trust)

**Turn 3 - Player Swipes (convinced):**
"The demon has a point. I won't raise taxes."

**Final Belief: +15 (positive = trusted)**

**Outcome:**
Since belief > 0, CLAIMED consequences apply:
- Wealth: -20 (demon's claim)
- Military: -5 (demon's claim)
- Stability: -15 (demon's claim)

Player avoided taxing but the military never got funded. Northern kingdom invades. Kingdom crumbles.
Player got manipulated by the demon's lies.

---

## Error Handling & Fallback

### Graceful Failure Strategy

**Goal:** API failures never break gameplay. If Groq times out or errors, fallback systems keep the game running.

### API Timeout/Failure

**Trigger:** Groq API doesn't respond within 5 seconds or returns error status

**Response:**
1. Log error for debugging/monitoring
2. Do NOT freeze or hang the game
3. Retrieve random fallback message from library (matching current deception mode)
4. Display fallback as if Groq had responded
5. Continue conversation with deterministic (non-API) belief calculations
6. Player doesn't notice the difference—game flows smoothly

**From Player Perspective:**
- They send message → typing indicator shows
- After ~5 seconds, get a response (fallback if API failed)
- Continue conversation normally
- No error message or warning needed

### Missing Configuration

**Scenario:** DemonConfig ScriptableObject not found in Resources folder

**Fallback Behavior:**
- Don't crash
- Log warning
- Load hardcoded defaults:
  - truthRatio: 50
  - lieRatio: 40
  - warningRatio: 50
  - confuseRatio: 10
  - maxExchanges: 4
  - Model: mixtral-8x7b-32768
- Demon still appears and works, just with default values

### Invalid Groq Response

**Scenario:** API returns malformed JSON or empty response

**Handling:**
- Attempt to parse response
- If parsing fails OR response has no choices, log error
- Use fallback message instead
- Belief calculations continue normally
- Next exchange uses fresh API call (transient failures often resolve)

### Fallback Message Library by Deception Mode

The system maintains a curated set of fallback messages for each mode:

**TRUTH Mode Fallbacks:**
- "The consequences are as you suspect, ruler."
- "Reality bends to your choices, willingly or not."
- "The dice have fallen. Accept what comes."
- "Truth, however bitter, is served cold."

**LIE Mode Fallbacks:**
- "Your triumph is assured... or so it seems."
- "This choice will fill your coffers beyond measure!"
- "Your enemies shall tremble at this stroke!"
- "A most... fortuitous decision, ruler."

**WARNING Mode Fallbacks:**
- "Beware! Ruin lurks in every shadow."
- "This path leads only to despair and ash."
- "The consequences are dire beyond imagining..."
- "Caution, ruler. The abyss watches."

**CONFUSE Mode Fallbacks:**
- "The left hand knows not what the right hand does..."
- "Is this choice, or is choice but illusion?"
- "The Whisper speaks in riddles only the wise may solve."
- "Three doors, three fates, or perhaps none at all."

### Network Configuration

**Allowed Domains:**
- api.groq.com (primary)
- Fall back to region-specific endpoints if primary is slow

**Rate Limiting:**
- Groq typically allows 100+ requests per minute for standard tiers
- Game makes 1 request per player message (not per keystroke)
- No risk of hitting limits in normal play
- Consider batching for large-scale deployments

### Recovery Strategy

**For Persistent API Issues:**

1. Check firewall/network settings
2. Verify Groq API key is valid (non-expired)
3. Check Groq dashboard for outages
4. Ensure model name (mixtral-8x7b-32768) is still available
5. Fallback mode ensures game remains playable during service disruptions

---

## Testing & Tuning

### Testing Checklist

- [ ] Demon overlay appears after card choice
- [ ] Player can type multi-turn responses (1-4 exchanges)
- [ ] Groq API responds within timeout window
- [ ] Belief score updates correctly per exchange
- [ ] Consequences apply based on final belief score
- [ ] True consequences used if player distrusts
- [ ] Demon consequences used if player trusts
- [ ] Fallback commentary triggers when API fails
- [ ] Conversation history maintained across turns
- [ ] Character limit enforced (500 chars)
- [ ] Typing indicator displays while API processes
- [ ] Overlay fades in/out smoothly
- [ ] Player can swipe to dismiss conversation
- [ ] Next card loads after demon interaction ends
- [ ] Level system gates cards correctly

### Tuning Parameters (in DemonConfig)

**Conversation Depth:**
- maxExchanges: 4 → Lower for quick gameplay (2 exchanges), higher for deep RP (5-6)
- characterPerTurnLimit: 500 → Adjust based on UI space available

**Belief Sensitivity:**
- beliefChangePerExchange: 15 → Base trust shift. Higher = more dramatic swings
- beliefBonusForDemonContradiction: 25 → Reward for catching inconsistencies
- beliefPenaltyForTruthfulDemon: -10 → Player paranoia when demon is honest (counterintuitive but fun)

**Demon Honesty:**
- truthRatio: 50 → % chance demon tells truth
- lieRatio: 40 → % chance demon lies
- warningRatio: 50 → % chance demon warns falsely
- confuseRatio: 10 → % chance demon speaks in riddles

**Late-Game Chaos:**
- peakLieLevel: 50 → At what player level demon becomes most deceptive
- lateLieBonus: +30 → How many more points demon gets to lie at peak

### Example Tuning Sessions

**For Story-Heavy Game (Deep RP & Roleplay):**
- maxExchanges: 4 (players want depth)
- beliefChangePerExchange: 10 (subtle shifts, not jarring)
- truthRatio: 60 (demon builds trust initially, more honest)
- lieRatio: 25 (less deceptive)
- characterPerTurnLimit: 750 (allow longer player responses)
- lateLieBonus: 20 (gradual corruption, not sudden)

**For Challenge/Puzzle Game:**
- maxExchanges: 2 (quick decisions, high stakes)
- beliefChangePerExchange: 20 (dramatic belief swings)
- truthRatio: 40 (demon mostly deceptive from start)
- lieRatio: 50 (high lie rate, punishes trust)
- characterPerTurnLimit: 300 (force concise player choices)
- lateLieBonus: 50 (extreme late-game chaos, demon becomes unreliable)

**For Horror/Paranoia Game:**
- maxExchanges: 3 (fewer exchanges = less time to discern truth)
- beliefChangePerExchange: 25 (wild belief swings)
- truthRatio: 30 (demon rarely honest)
- warningRatio: 70 (lots of false alarms, constant paranoia)
- beliefPenaltyForTruthfulDemon: -20 (player's paranoia is strong—hard to trust even when demon honest)
- lateLieBonus: 40 (demon's influence grows)

---

## Summary Table

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Chat UI** | Unity UI Canvas + InputField | Player input & demon responses |
| **Conversation State** | ConversationTurn + DemonChatSession | Multi-turn history tracking |
| **Belief System** | Trust score (-100 to +100) | Determines consequence application |
| **Groq Integration** | HTTP REST calls | Generate demon responses |
| **Deception Logic** | DeceptionMode enum + ratios | Control truth/lie probability |
| **Consequence System** | GameConsequence + ApplyOutcome() | Resource changes based on belief |
| **Level Gating** | CardLevelDetector | Unlock cards by progression |

---

## Key Insights

✅ **Belief > Truth**: What matters is whether player *believes* the demon, not what demon said
✅ **Multi-Turn Depth**: Players can challenge, learn, and adjust trust over 1-4 exchanges
✅ **Groq as Conversation Partner**: Full chat history passed to API for consistent persona
✅ **No Game Logic in Prompts**: Deception mode and consequences defined in code, not prompt
✅ **Fallback Ready**: API failures don't break gameplay, just use static text
✅ **Overlay UX**: Demon appears on top of card, player can swipe to dismiss

---

## Getting Started Checklist

1. **Create DemonConfig** in inspector (set truthRatio, lieRatio, etc.)
2. **Get Groq API Key** from groq.com and add to config
3. **Build GroqService.cs** with error handling
4. **Create DemonChatPanel** UI (portrait, text, input field)
5. **Implement DemonChatManager** (conversation flow)
6. **Add isDemonCard** flag to CardData assets
7. **Integrate with GameManager.OnPlayerChoice()**
8. **Test first card conversation** (1-2 exchanges)
9. **Tune belief ratios** based on playtest feedback
10. **Deploy & Monitor** API latency and error rates

---

## Implementation Checklist

**Phase 1: Core Systems Setup**
- [ ] Create DemonConfig ScriptableObject
- [ ] Set up Groq API key (from groq.com)
- [ ] Build GroqService with HTTP request handling
- [ ] Implement error handling and fallback system
- [ ] Create data structures (ConversationTurn, DemonChatSession, etc.)

**Phase 2: Conversation Flow**
- [ ] Implement DemonPersonaManager (deception mode selection)
- [ ] Build DemonChatManager (session orchestration)
- [ ] Create DemonChatPanel UI (overlay, chat display, input)
- [ ] Integrate with GameManager.OnPlayerChoice()

**Phase 3: Belief System**
- [ ] Implement belief score calculation
- [ ] Create trust delta analysis (contradiction detection, language analysis)
- [ ] Build consequence application logic
- [ ] Create optional BeliefMeterUI for visual feedback

**Phase 4: Testing & Tuning**
- [ ] Test first card conversation (1-2 exchanges)
- [ ] Verify Groq API latency acceptable
- [ ] Test fallback messages on API timeout
- [ ] Playtest belief system (does it feel fair?)
- [ ] Tune ratios based on game feel

**Phase 5: Polish**
- [ ] Add visual effects (fade animations, demon portrait)
- [ ] Sound design (typing sounds, demon voice effects)
- [ ] Create compelling demon-specific card chains
- [ ] Implement demon betrayal or corruption arc

---

## Directory Structure

**Core Systems:**
- GroqService: HTTP API wrapper
- DemonPersonaManager: Mode selection and consequence generation
- DemonChatManager: Conversation orchestration
- CardLevelDetector: Level-based card unlocking
- AISystemManager: Singleton coordinator

**UI Layer:**
- DemonChatPanel: Overlay and chat interface
- DemonOverlayUI: Portrait and styling
- BeliefMeterUI: Optional belief visualization

**Data & Config:**
- DemonConfig: Tunable parameters
- ConversationTurn: Message history structure
- DemonChatSession: Session state tracking
- DeceptionMode: Enum and mode constants

**Assets:**
- DemonConfig.asset: Instance of ScriptableObject
- demon_system_prompt.txt: Template for Groq system message
- Demon portrait sprite

---

**Version 1.0** | Concept-to-Implementation Guide | No Code Included
