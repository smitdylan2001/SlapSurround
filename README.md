Inspiration
I always had an idea to use the world around me to make music. When real-time meshing was added to ARKit this was a dream to work with. My interest lies more in the VR/MR world, so when I heard scene mesh support for Quest 3 I could not wait to jump on it! I wanted to make an impressive and unique visual, which might seem simple, but has a very complex backend. This pushes the boundaries of what is possible in mixed reality, as this is not done before with particles.

What it does
Slap Surround is an immersive music experience. Once you see your hands, you can hit your surroundings, which places a note. The speed at which you hit your surroundings decides how your note sounds and how the particles behave.

How we built it
I use Unity 2022LTS with the Universal Render pipeline to make the project. To improve immersion I use occlusion on the scene mesh to occlude particles outside of your room. I use a custom job system to read out mesh data for the particle placement. These jobs are optimized with Unity Burst for Android ARM64-v9. This system returns an array, which writes to a graphics buffer for VFX graph to read and spawn the particles.

Challenges we ran into
Getting particles to pulse and spawn onto the world mesh was the biggest challenge. Randomly sampling a mesh is easily doable in VFX graph, but the making a wave on top of the scene mesh seemed impossible. Shaders do this by distance, but a particle needs an exact start position and direction. To solve this, I had to loop over all vertices once per spawned particle. Doing this once is relatively simple and can run in C#, but doing this for 50 times per music note will kill performance. It can easily look over each vertex 300+ times. I took the performance from about 5 fps to a comfortable 72 fps by optimizing the system as much as I could. The solution I found was to use the Unity job system with Burst and the Unity MeshData API to calculate the closest vertex per particle and copy the position into a native array. This array is passed from C# to a graphics buffer in VFX graph to spawn and place these particles.

Accomplishments that we're proud of
Getting a custom mesh reading feature to work with VFX graph is the accomplishment I am most proud of. The flow for this is described above. I had to take performance from under 5 fps to 72 fps on quest 3, utilizing the full CPU. I even got it to read out the normals of the mesh as well to shoot the music particles in the direction of the mesh, instead of just up. This adds a lot of immersion. This enabled many fun interactions, resulting in the music experience.

What we learned
VFX graph is amazing, but it has limits
The Job System can be a savior for CPU performance because of multithreading
The mesh data API is really cool! This can get easy access to mesh data
The Scene Mesh is is really easy to get started with and can make great interactions
What's next for Slap Surround
I want to post this to my network to see what people think. If there is enough interest, time, and budget I would love to make a full music game with this mechanic! Working with the scene mesh was great, so I cannot wait to develop more unique interactions and visuals with this.
