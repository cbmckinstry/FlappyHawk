# Hawkeye Flappy — Unity Project

A **Flappy Bird–style endless runner** built in **Unity**, themed around the University of Iowa Hawkeyes. The project includes dynamic difficulty, synchronized parallax backgrounds, and data logging for every play session.

---

## Gameplay Overview

The player controls a bird that flaps upward against gravity. Avoid pipes to score points.

### Controls
- `Space` / Left Click / Gamepad South Button → Flap
- `ESC` → Quit Game

### Game Flow
1. Press **Play** to start a round.  
2. Flap between pipes to score points.  
3. Game ends on collision.  
4. Press **Difficulty** to toggle between Easy → Normal → Hard.  

---

## Difficulty System

Difficulty only affects **pipe spawn frequency**, not movement speed.

| Mode | Spawn Interval | Gravity | Description |
|------|----------------|----------|--------------|
| Easy | 1.15 s | -9.8 | Wide spacing, easy pace |
| Normal | 1.0 s | -9.8 | Standard gameplay |
| Hard | 0.85 s | -9.8 | Tight spacing, high difficulty |

### Events
- `OnSpawnRateChanged`: Updates spawn frequency.  
- `OnPipeSpeedChanged`: Keeps pipes and parallax in sync.  

---

## Core Scripts

### **GameManager.cs**
Central controller for difficulty, scoring, and logging.  
Handles pause, play, and difficulty changes.

**Key Responsibilities**
- Tracks score, jumps, elapsed time, and pipes spawned.  
- Controls gravity and pipe spawn frequency per difficulty.  
- Broadcasts events for parallax and spawner updates.  
- Logs every completed round through `RunDataLogger`.  

**Important Fields**
```csharp
public static event Action<float> OnPipeSpeedChanged;
public static event Action<float> OnSpawnRateChanged;
public float CurrentSpawnRate { get; private set; }
public float CurrentPipeSpeed { get; private set; }
```

---

### **Spawner.cs**
Spawns pipe prefabs at random heights with a variable rate.

- Subscribes to `GameManager.OnSpawnRateChanged`.  
- Automatically adjusts spawn timing when difficulty changes.  
- Calls `RegisterPipe()` in `GameManager` for data logging.

```csharp
if (timer >= spawnRate) {
    timer = 0f;
    SpawnPipe();
}
```

---

### **Pipes.cs**
Moves each pipe left at a constant speed and destroys it when off-screen.

- Listens to `OnPipeSpeedChanged` for consistency.  
- Syncs pipe motion with parallax and ground.

---

### **Player.cs**
Implements the bird’s physics and controls.

- Flaps on space/click/gamepad input.  
- Manual gravity instead of Unity physics.  
- Collisions:
  - `Obstacle` → Game Over  
  - `Scoring` → Increase Score  
- Animates wings by cycling sprites.

---

### **Parallax.cs**
Handles background and ground movement for depth illusion.

- `matchPipesExactly = true` → Ground scrolls at pipe speed.  
- `parallaxRatio = 0.25f` → Background scrolls slower for depth.  
- Automatically pauses during Game Over or Pause.

---

### **RunDataLogger.cs**
Creates and maintains a CSV file with gameplay analytics.

**Stored fields:**
- `player_id` (persistent unique identifier)  
- `difficulty`  
- `score`  
- `round_seconds`  
- `start_utc`  
- `pipes_spawned`  
- `jumps`  

Logs are saved to:
```
Documents/FlappyBird/Logs/game_runs.csv
```

Fallback path: `Application.persistentDataPath` (cross-platform).

---

## Technical Details

- **Engine:** Unity 6000.2.2f1 (Unity 6)  
- **Frame Rate:** Locked at 60 FPS  
- **Gravity:** Manually applied each frame (`direction.y += gravity * Time.deltaTime;`)  
- **Pause System:** Controlled via `Time.timeScale = 0`  
- **Save Format:** CSV for universal compatibility  

---

## Event Flow

| Event | Raised By | Listened By | Effect |
|--------|------------|-------------|---------|
| `OnPipeSpeedChanged` | `GameManager` | `Pipes`, `Parallax` | Sync movement speed |
| `OnSpawnRateChanged` | `GameManager` | `Spawner` | Adjust spawn frequency |
| `IncreaseScore()` | `Player` | — | Increments player score |
| `GameOver()` | `Player` | `GameManager` | Stops time and logs run |

---

## Data Logging Example

Example entry from `game_runs.csv`:

```
player_id,difficulty,score,round_seconds,start_utc,pipes_spawned,jumps
c52e6b4dbab344e79d77a8c34fc2738e,Normal,11,13.785,2025-10-21T03:41:46.5265220Z,12,36
```

---

## Future Enhancements

- Add sound and music.  
- Add animated background layers.  
- Implement leaderboard using the CSV data.  
- Add restart button on Game Over screen.  

---
