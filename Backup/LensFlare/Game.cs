#region File Description
//-----------------------------------------------------------------------------
// Game.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace LensFlare
{
    /// <summary>
    /// Sample showing how to implement a lensflare effect, using occlusion
    /// queries to hide the flares when the sun is hidden behind the landscape.
    /// </summary>
    public class LensFlareGame : Microsoft.Xna.Framework.Game
    {
        #region Fields

        GraphicsDeviceManager graphics;

        KeyboardState currentKeyboardState = new KeyboardState();
        GamePadState currentGamePadState = new GamePadState();
        
        Vector3 cameraPosition = new Vector3(-200, 50, 0);
        Vector3 cameraFront = new Vector3(1, 0, 0);

        Model terrain;

        LensFlareComponent lensFlare;

        private PlanarWater water;

        private RenderTarget2D sceneDepth;
        private RenderTarget2D waterReflection;
        private RenderTarget2D sceneColor;

        private Effect RenderDepth;

        private Skybox Sky;

        #endregion

        #region Initialization


        public LensFlareGame()
        {
            graphics = new GraphicsDeviceManager(this);

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;

            Content.RootDirectory = "Content";

            // Create and add the lensflare component.
            lensFlare = new LensFlareComponent(this);

            Components.Add(lensFlare);
        }


        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            terrain = Content.Load<Model>("terrain");

            water = new PlanarWater(graphics.GraphicsDevice, Content.Load<Effect>("Water"),
                                    Content.Load<Texture2D>("WaterDUDV"), Content.Load<Texture2D>("WaterNormal"));

            int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            sceneDepth = new RenderTarget2D(GraphicsDevice, width, height, 1, SurfaceFormat.Single,
                                            RenderTargetUsage.DiscardContents);
            waterReflection = new RenderTarget2D(GraphicsDevice, width, height, 1, SurfaceFormat.Color,
                                                 RenderTargetUsage.DiscardContents);
            sceneColor = new RenderTarget2D(GraphicsDevice, width/2, height/2, 1, SurfaceFormat.Color,
                                            RenderTargetUsage.DiscardContents);

            this.RenderDepth = Content.Load<Effect>("SceneDepth");

            this.Sky = new Skybox(GraphicsDevice, Content.Load<TextureCube>("Miramar"), Content.Load<Effect>("Skybox"));
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Allows the game to run logic.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            UpdateCamera(gameTime);

            base.Update(gameTime);
        }

        protected void DrawTerrain(Effect customEffect, ref Matrix view, ref Matrix projection)
        {
            Queue<BasicEffect> meshPartEffects = null;

            if (customEffect != null)
            {
                meshPartEffects = new Queue<BasicEffect>();
                customEffect.Parameters["View"].SetValue(view);
                customEffect.Parameters["Projection"].SetValue(projection);
            }

            foreach (ModelMesh mesh in terrain.Meshes)
            {
                if (customEffect == null)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = Matrix.Identity;
                        effect.View = view;
                        effect.Projection = projection;

                        effect.LightingEnabled = true;
                        effect.DiffuseColor = new Vector3(1f);
                        effect.AmbientLightColor = new Vector3(0.5f);

                        effect.DirectionalLight0.Enabled = true;
                        effect.DirectionalLight0.DiffuseColor = Vector3.One;
                        effect.DirectionalLight0.Direction = lensFlare.LightDirection;

                        effect.FogEnabled = true;
                        effect.FogStart = 300;
                        effect.FogEnd = 1000;
                        effect.FogColor = Color.CornflowerBlue.ToVector3();
                    }
                }
                else
                {
                    foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    {
                        meshPartEffects.Enqueue(meshPart.Effect as BasicEffect);

                        meshPart.Effect = this.RenderDepth;
                    }
                }

                mesh.Draw();
            }

            if (meshPartEffects == null)
                return;

            foreach (ModelMesh mesh in terrain.Meshes)
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    meshPart.Effect = meshPartEffects.Dequeue();
        }

        private void DrawReflection(float aspectRatio, out Matrix reflectionViewProjection)
        {
            // Set target to reflection and clear it
            GraphicsDevice.SetRenderTarget(0, this.waterReflection);
            GraphicsDevice.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.CornflowerBlue, 1f, 0);

            const float waterHeight = 24f;

            // Get a virtual camera position
            Vector3 reflectionCamPosition = new Vector3
            {
                X = cameraPosition.X,
                Y = waterHeight - (cameraPosition.Y - waterHeight),
                Z = cameraPosition.Z
            };

            // Reflect the current cameraDirection around the water plane
            Vector3 reflectedDir = Vector3.Reflect(Vector3.Normalize(cameraFront), Vector3.Up);

            Vector3 reflectionCamTarget = default(Vector3);
            reflectionCamTarget.X = reflectionCamPosition.X + reflectedDir.X;
            reflectionCamTarget.Y = reflectionCamPosition.Y + reflectedDir.Y;
            reflectionCamTarget.Z = reflectionCamPosition.Z + reflectedDir.Z;

            // Generate view and projection matrices using virtual position, target and a field of view of 90 degrees
            Matrix reflectionView = Matrix.CreateLookAt(reflectionCamPosition, reflectionCamTarget, Vector3.Up);
            Matrix reflectionProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, aspectRatio, 0.1f, 10000f);

            // Output combioned reflection view and projection matrices
            reflectionViewProjection = reflectionView * reflectionProjection;

            // Create a normalized world space clip plane representing the water
            Plane reflectionClipPlane = new Plane(Vector3.Up, -waterHeight);
            reflectionClipPlane.Normalize();

            // Transform the world space clip plane into clip space
            Plane reflectionClipPlaneWaterSpace;
            Plane.Transform(ref reflectionClipPlane, ref reflectionViewProjection, out reflectionClipPlaneWaterSpace);

            // Set the clip plane on the hardware device
            GraphicsDevice.ClipPlanes[0].Plane = reflectionClipPlaneWaterSpace;
            GraphicsDevice.ClipPlanes[0].IsEnabled = true;

            this.Sky.Draw(reflectionView, reflectionProjection);

            // Draw the terrain with our reflection view and projection
            this.DrawTerrain(null, ref reflectionView, ref reflectionProjection);

            // Make sure to unset to the clip plane, else further drawing will be spoilt
            GraphicsDevice.ClipPlanes[0].IsEnabled = false;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            // Compute camera matrices.
            Matrix view = Matrix.CreateLookAt(cameraPosition, cameraPosition + cameraFront, Vector3.Up);

            float aspectRatio = GraphicsDevice.Viewport.AspectRatio;
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 1000f);

            GraphicsDevice.SetRenderTarget(0, this.sceneColor);
            GraphicsDevice.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.CornflowerBlue, 1f, 0);

            this.DrawTerrain(null, ref view, ref projection);

            Matrix reflectionViewProjection;
            this.DrawReflection(aspectRatio, out reflectionViewProjection);

            GraphicsDevice.SetRenderTarget(0, this.sceneDepth);
            GraphicsDevice.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.White, 1f, 0);

            this.RenderDepth.Parameters["FarPlane"].SetValue(1000f - 0.1f);
            this.DrawTerrain(this.RenderDepth, ref view, ref projection);

            GraphicsDevice.SetRenderTarget(0, null);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            this.Sky.Draw(view, projection);

            this.DrawTerrain(null, ref view, ref projection);

            this.water.Draw(graphics.GraphicsDevice, this.sceneDepth.GetTexture(), this.waterReflection.GetTexture(),
                            this.sceneColor.GetTexture(), reflectionViewProjection, view, projection, (float)gameTime.TotalGameTime.TotalSeconds,
                           this.cameraPosition, lensFlare.LightDirection);
            
            // Tell the lensflare component where our camera is positioned.
            lensFlare.View = view;
            lensFlare.Projection = projection;

            base.Draw(gameTime);
        }


        #endregion

        #region Handle Input


        /// <summary>
        /// Handles input for quitting the game.
        /// </summary>
        private void HandleInput()
        {
            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // Check for exit.
            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentGamePadState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }
        }


        /// <summary>
        /// Handles camera input.
        /// </summary>
        private void UpdateCamera(GameTime gameTime)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            // Check for input to rotate the camera.
            float pitch = -currentGamePadState.ThumbSticks.Right.Y * time * 0.001f;
            float turn = -currentGamePadState.ThumbSticks.Right.X * time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Up))
                pitch += time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Down))
                pitch -= time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Left))
                turn += time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Right))
                turn -= time * 0.001f;

            Vector3 cameraRight = Vector3.Cross(Vector3.Up, cameraFront);
            Vector3 flatFront = Vector3.Cross(cameraRight, Vector3.Up);

            Matrix pitchMatrix = Matrix.CreateFromAxisAngle(cameraRight, pitch);
            Matrix turnMatrix = Matrix.CreateFromAxisAngle(Vector3.Up, turn);

            Vector3 tiltedFront = Vector3.TransformNormal(cameraFront, pitchMatrix * 
                                                          turnMatrix);

            // Check angle so we can't flip over.
            if (Vector3.Dot(tiltedFront, flatFront) > 0.001f)
            {
                cameraFront = Vector3.Normalize(tiltedFront);
            }

            Vector3 camPos = cameraPosition;

            // Check for input to move the camera around.
            if (currentKeyboardState.IsKeyDown(Keys.W))
                camPos += cameraFront * time * 0.1f;
            
            if (currentKeyboardState.IsKeyDown(Keys.S))
                camPos -= cameraFront * time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.A))
                camPos += cameraRight * time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.D))
                camPos -= cameraRight * time * 0.1f;

            camPos += cameraFront *
                              currentGamePadState.ThumbSticks.Left.Y * time * 0.1f;

            camPos -= cameraRight *
                              currentGamePadState.ThumbSticks.Left.X * time * 0.1f;

            cameraPosition = Vector3.Lerp(cameraPosition, camPos, 0.5f);

            if (currentGamePadState.Buttons.RightStick == ButtonState.Pressed ||
                currentKeyboardState.IsKeyDown(Keys.R))
            {
                cameraPosition = new Vector3(-200, 30, 30);
                cameraFront = new Vector3(1, 0, 0);
            }
        }


        #endregion
    }


    #region Entry Point

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static class Program
    {
        static void Main()
        {
            using (LensFlareGame game = new LensFlareGame())
            {
                game.Run();
            }
        }
    }

    #endregion
}
