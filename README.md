# The Last Astronaut

A 2D top-down astronaut-versus-alien wave survival shooter built in Unity for the GMTK Game Jam, featuring character and weapon selection, escalating enemy waves, a power-up system, and a global leaderboard.

# Play here

tytygamedev.itch.io/the-last-astronaut
![Gameplay screenshot](Assets/Images/thumb image.png)
![Gameplay screenshot](Assets/Images/GameplayGif.gif)

# Controls

Move: WASD / Arrow Keys
Shoot: Mouse Button left
Deflect: Mouse Button Right
Dash: Shift

## About

This project was built for the GMTK Game Jam as a collaborative effort. It puts you in the boots of a stranded astronaut fending off waves of increasingly dangerous aliens, combining fast-paced top-down shooting with run-defining power-up choices and local high score tracking.

## Features

Wave-based enemy spawning with escalating difficulty
Multiple enemy types, including ranged shooting enemies
Character and weapon selection on the start screen
Randomized power-up system with in-game selection UI
HUD with live health tracking, death screen, and local high scores
Original pixel-art assets and soundtrack

## Built With

Unity 6
C#
Unity's new Input System
TextMeshPro for UI text rendering
Rigidbody2D-based physics

## What We Learned

Coordinating cross-system game state through singleton managers (WaveManager, ScoreManager, MusicManager)
Wiring runtime UI and spawned objects cleanly between scene objects, prefabs, and manager scripts
Diagnosing and fixing Unity lifecycle ordering issues between Awake() and Start()
Handling collision timing issues (e.g. proactive vs. reactive IgnoreCollision calls) for reliable bullet physics
Coordinating a multi-person Git workflow across branches under jam time pressure
