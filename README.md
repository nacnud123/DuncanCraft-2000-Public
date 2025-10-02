# DuncanCraft 2000

DuncanCraft 2000 is a Minecraft classic clone created in C# using OpenTK. It has fast infinite worlds, saving/loading, caves, blocks, bugs, and UI. This was created as a exploration of using no game enigne to make voxel games. Here you will find the latest build of the game along with the source code.

## Controls

W A S D - Move around  
Left click - Break block  
Right click - Place blocks  
Scroll wheel - Change selected block  
1 - 0 - Also change selected block  
X - Toggle wireframe mode  
I - Toggle inventory  
Esc - Pause / Exit inventory

### Disclaimer

I still consider this project to be in active development, so expect a few bugs here and there. If you find one, please report it as an issue. Thanks!  

<img width="1915" height="1006" alt="NewLighting1" src="https://github.com/user-attachments/assets/a6ed2759-c7a4-43a3-8b31-7c27e91d3339" />
<img width="1911" height="1000" alt="NewLighting2" src="https://github.com/user-attachments/assets/9e461a17-560f-4de9-bfd2-c1b6213cb516" />
<img width="1911" height="1000" alt="NewLighting3" src="https://github.com/user-attachments/assets/af309b46-6142-43d9-a242-bd25ead96596" />

### Known Bugs

Sometimes when breaking or placing a block, the chunk will temporarily become invisible or completely black. This happens because of how I am performing mesh rendering; there is a slight delay between the chunk information being calculated and it being uploaded to the GPU.  
Sometimes, when breaking or placing a block, the respective breaking sound will not play.  
