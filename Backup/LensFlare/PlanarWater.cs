using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace LensFlare
{
    public class PlanarWater
    {
        private VertexBuffer PlaneVB;
        private VertexDeclaration PlaneVD;

        private Effect WaterEffect;

        /// <summary>
        /// A tesselation factor to balance the texture read cache.
        /// </summary>
        private const int Tesselation = 10;

        private const int TesselationN1 = Tesselation - 1;

        private Texture2D OffsetMap;
        private Texture2D NormalMap;

        /// <summary>
        /// Static world matrix that will match our water grid up to terrain
        /// </summary>
        private static Matrix World;

        static PlanarWater()
        {
            PlanarWater.World = Matrix.CreateScale(600f, 1f, 600f) * Matrix.CreateTranslation(25f, 24f, 25f);
        }

        public PlanarWater(GraphicsDevice device, Effect waterEffect, Texture2D offsetMap, Texture2D normalMap)
        {
            this.OffsetMap = offsetMap;
            this.NormalMap = normalMap;

            const int vertexCount = 6 * TesselationN1 * TesselationN1;
            
            this.WaterEffect = waterEffect;
            this.PlaneVB = new VertexBuffer(device, typeof(VertexPositionTexture), vertexCount, BufferUsage.WriteOnly);
            this.PlaneVD = new VertexDeclaration(device, VertexPositionTexture.VertexElements);

            VertexPositionTexture[] vertices = new VertexPositionTexture[vertexCount];

            // Space between grid cells
            const float delta = 1f/Tesselation;

            // Offset to position around origin in object space
            const float halfLength = 0.5f;

            // current index into vertices
            int index = 0;

            for (int x = 0; x < Tesselation-1; x++)
            {
                // positions for current and next along X
                float column1 = (delta * x) - halfLength;
                float column2 = column1  + delta;

                // texcoords for current and next along U
                float uCoord1 = column1 + 0.5f;
                float uCoord2 = column2 + 0.5f;

                for (int z = 0; z < Tesselation-1; z++)
                {
                    // positions for current and next along Z
                    float row1 = (delta * z) - halfLength;
                    float row2 = row1 + delta;

                    // texcoords for current and next along V
                    float vCoord1 = row1 + 0.5f;
                    float vCoord2 = row2 + 0.5f;

                    /// Triangle 1
                    {
                        vertices[index].TextureCoordinate = new Vector2(uCoord1, vCoord1);
                        vertices[index++].Position = new Vector3(column1, 0, row1);

                        vertices[index].TextureCoordinate = new Vector2(uCoord2, vCoord1);
                        vertices[index++].Position = new Vector3(column2, 0, row1);

                        vertices[index].TextureCoordinate = new Vector2(uCoord2, vCoord2);
                        vertices[index++].Position = new Vector3(column2, 0, row2);
                    }

                    /// Triangle 2
                    {
                        vertices[index].TextureCoordinate = new Vector2(uCoord2, vCoord2);
                        vertices[index++].Position = new Vector3(column2, 0, row2);

                        vertices[index].TextureCoordinate = new Vector2(uCoord1, vCoord2);
                        vertices[index++].Position = new Vector3(column1, 0, row2);

                        vertices[index].TextureCoordinate = new Vector2(uCoord1, vCoord1);
                        vertices[index++].Position = new Vector3(column1, 0, row1);
                    }
                }
            }

            this.PlaneVB.SetData(vertices);
        }

        public struct TweetDataBlock
        {
            public uint ImageIndex;
            public string Name;
            public string Description;
        }

        public void Draw(GraphicsDevice device, Texture2D sceneDepth, Texture2D waterReflection, Texture2D sceneColor,
                         Matrix waterViewProjection, Matrix view, Matrix projection, float time,
                         Vector3 cameraPosition, Vector3 lightDirection)
        {
            device.VertexDeclaration = this.PlaneVD;
            device.Vertices[0].SetSource(this.PlaneVB, 0, VertexPositionTexture.SizeInBytes);
            device.Indices = null;

            device.Textures[0] = sceneDepth;
            device.Textures[1] = waterReflection;
            device.Textures[2] = sceneColor;
            device.Textures[3] = this.OffsetMap;
            device.Textures[4] = this.NormalMap;

            for (int i = 0; i < 3; i++)
            {
                device.SamplerStates[i].AddressU = TextureAddressMode.Clamp;
                device.SamplerStates[i].AddressV = TextureAddressMode.Clamp;
            }

            device.SamplerStates[0].MinFilter = TextureFilter.Point;
            device.SamplerStates[0].MagFilter = TextureFilter.Point;
            device.SamplerStates[0].MipFilter = TextureFilter.Point;

            RenderState renderState = device.RenderState;

            renderState.DepthBufferEnable = true;
            renderState.AlphaTestEnable = false;
            renderState.AlphaBlendEnable = false;

            this.WaterEffect.Begin();

            this.WaterEffect.Parameters["World"].SetValue(PlanarWater.World);
            this.WaterEffect.Parameters["View"].SetValue(view);
            this.WaterEffect.Parameters["Projection"].SetValue(projection);
            this.WaterEffect.Parameters["WaterViewProjection"].SetValue(waterViewProjection);
            this.WaterEffect.Parameters["time"].SetValue(time);
            this.WaterEffect.Parameters["CameraPosition"].SetValue(cameraPosition);
            this.WaterEffect.Parameters["LightDirection"].SetValue(lightDirection);

            this.WaterEffect.Techniques[0].Passes[0].Begin();

            const int primitiveCount = 2 * (TesselationN1*TesselationN1);
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, primitiveCount);

            this.WaterEffect.Techniques[0].Passes[0].End();
            this.WaterEffect.End();

            device.Textures[0] = null;
            device.Textures[1] = null;
            device.Textures[2] = null;
            device.Textures[3] = null;
            device.Textures[4] = null;

            for (int i = 0; i < 3; i++)
            {
                device.SamplerStates[i].AddressU = TextureAddressMode.Wrap;
                device.SamplerStates[i].AddressV = TextureAddressMode.Wrap;
            }

            device.SamplerStates[0].MinFilter = TextureFilter.Linear;
            device.SamplerStates[0].MagFilter = TextureFilter.Linear;
            device.SamplerStates[0].MipFilter = TextureFilter.Linear;
        }
    }
}