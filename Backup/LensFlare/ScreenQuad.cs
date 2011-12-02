using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LensFlare
{
    public class ScreenQuad
    {
        private readonly VertexDeclaration VertexDeclaration;

        private readonly VertexPositionTexture[] Vertices;
        private readonly short[] Indices;

        public Vector2 HalfPixel { get; private set; }

        public ScreenQuad(GraphicsDevice device)
        {
            this.VertexDeclaration = new VertexDeclaration(device, VertexPositionTexture.VertexElements);

            this.Vertices = new[]
            {
                new VertexPositionTexture(Vector3.Zero, Vector2.One),
                new VertexPositionTexture(Vector3.Zero, Vector2.UnitY),
                new VertexPositionTexture(Vector3.Zero, Vector2.Zero),
                new VertexPositionTexture(Vector3.Zero, Vector2.UnitX)
            };

            this.Indices = new short[]
            {
                0, 1, 2, 2, 3, 0
            };

            this.HalfPixel = new Vector2(0.5f / device.PresentationParameters.BackBufferWidth,
                                          -0.5f / device.PresentationParameters.BackBufferHeight);
        }

        public void Render(GraphicsDevice device, Vector2 v1, Vector2 v2)
        {
            device.VertexDeclaration = this.VertexDeclaration;

            this.Vertices[0].Position.X = v2.X - this.HalfPixel.X;
            this.Vertices[0].Position.Y = v1.Y - this.HalfPixel.Y;


            this.Vertices[1].Position.X = v1.X - this.HalfPixel.X;
            this.Vertices[1].Position.Y = v1.Y - this.HalfPixel.Y;

            this.Vertices[2].Position.X = v1.X - this.HalfPixel.X;
            this.Vertices[2].Position.Y = v2.Y - this.HalfPixel.Y;

            this.Vertices[3].Position.X = v2.X - this.HalfPixel.X;
            this.Vertices[3].Position.Y = v2.Y - this.HalfPixel.Y;

            CullMode cm = device.RenderState.CullMode;

            device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, this.Vertices, 0, 4, this.Indices, 0, 2);

            device.RenderState.CullMode = cm;
        }
    }
}