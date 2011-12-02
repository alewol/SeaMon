using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LensFlare
{
    class Skybox
    {
        private VertexDeclaration VertexDeclaration;
        private VertexBuffer VertexBuffer;
        private IndexBuffer IndexBuffer;

        private TextureCube Environment;
        private Effect Shader;

        private GraphicsDevice Device;

        public Skybox(GraphicsDevice device, TextureCube environment, Effect shader)
        {
            this.Environment = environment;
            this.Shader = shader;

            this.Device = device;

            this.VertexDeclaration = new VertexDeclaration(device, VertexPositionColor.VertexElements);
            this.VertexBuffer = new VertexBuffer(device, typeof(VertexPositionColor), 8, BufferUsage.WriteOnly);
            this.IndexBuffer = new IndexBuffer(device, 36 * sizeof(ushort), BufferUsage.WriteOnly, IndexElementSize.SixteenBits);

            VertexPositionColor[] vertices = new VertexPositionColor[8];
            vertices[0].Position = new Vector3(-1, 1, 1);
            vertices[1].Position = new Vector3(1, 1, 1);
            vertices[2].Position = new Vector3(1, 1, -1);
            vertices[3].Position = new Vector3(-1, 1, -1);

            vertices[4].Position = new Vector3(-1, -1, 1);
            vertices[5].Position = new Vector3(1, -1, 1);
            vertices[6].Position = new Vector3(1, -1, -1);
            vertices[7].Position = new Vector3(-1, -1, -1);

            this.VertexBuffer.SetData(vertices, 0, 8);

            vertices = null;

            ushort[] indices = new ushort[36];

            // top
            indices[0] = 0; indices[1] = 1; indices[2] = 3;
            indices[3] = 2; indices[4] = 3; indices[5] = 1;

            //bottom
            indices[6] = 5; indices[7] = 4; indices[8] = 6;
            indices[9] = 7; indices[10] = 6; indices[11] = 4;

            // front
            indices[12] = 3; indices[13] = 2; indices[14] = 7;
            indices[15] = 6; indices[16] = 7; indices[17] = 2;

            // back
            indices[18] = 1; indices[19] = 0; indices[20] = 4;
            indices[21] = 4; indices[22] = 5; indices[23] = 1;

            // left
            indices[24] = 0; indices[25] = 3; indices[26] = 4;
            indices[27] = 4; indices[28] = 3; indices[29] = 7;

            // right
            indices[30] = 2; indices[31] = 1; indices[32] = 6;
            indices[33] = 5; indices[34] = 6; indices[35] = 1;

            this.IndexBuffer.SetData(indices, 0, 36);

            indices = null;
        }

        ~Skybox()
        {
            this.VertexDeclaration.Dispose();
            this.VertexDeclaration = null;

            this.VertexBuffer.Dispose();
            this.VertexBuffer = null;

            this.IndexBuffer.Dispose();
            this.IndexBuffer = null;

            this.Device = null;

            this.Environment.Dispose();
            this.Environment = null;

            this.Shader.Dispose();
            this.Shader = null;
        }

        public void Draw(Matrix view, Matrix projection)
        {
            view.Translation = Vector3.Zero;

            Matrix viewProjection = view*projection;

            this.Device.VertexDeclaration = this.VertexDeclaration;
            this.Device.Vertices[0].SetSource(this.VertexBuffer, 0, VertexPositionColor.SizeInBytes);
            this.Device.Indices = this.IndexBuffer;

            RenderState state = Device.RenderState;

            state.DepthBufferEnable = false;
            state.DepthBufferWriteEnable = false;

            state.CullMode = CullMode.None;

            state.AlphaBlendEnable = false;
            state.AlphaTestEnable = false;

            this.Device.Textures[0] = this.Environment;

            this.Shader.Begin();
            this.Shader.Parameters["ViewProjection"].SetValue(viewProjection);

            this.Shader.Techniques[0].Passes[0].Begin();
            this.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 8, 0, 12);

            this.Shader.Techniques[0].Passes[0].End();

            this.Shader.End();

            state.DepthBufferEnable = true;
            state.DepthBufferWriteEnable = true;

            state.CullMode = CullMode.CullCounterClockwiseFace;
        }
    }
}
