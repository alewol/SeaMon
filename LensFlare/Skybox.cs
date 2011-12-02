using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace LensFlare
{
    /// <summary>  
    /// Custom vertex type for vertices which have a position, normal  
    /// and a color.  
    /// </summary>  
    [StructLayout(LayoutKind.Explicit, Size = 36)] 
    public struct VertexPositionColorNormal : IVertexType
    {

        /// <summary>  
        /// Position of the vertex.  
        /// </summary>  
        [FieldOffset(0)]
        public Vector3 Position;

        /// <summary>  
        /// The normal vector for this vertex.  
        /// </summary>  
        [FieldOffset(12)]
        public Vector3 Normal;

        [FieldOffset(24)]
        public Vector2 TextureCoordinate;

        /// <summary>  
        /// The color at this vertex.  
        /// </summary>  
        [FieldOffset(32)]
        public Color Color;

        /// <summary>  
        /// Constructor.  
        /// </summary>  
        /// <param name="pos">Position of the vertex.</param>  
        /// <param name="norm">Normal vector of the vertex.</param>  
        /// <param name="col">Color at the vertex.</param>  
        public VertexPositionColorNormal(Vector3 pos, Vector3 norm, Color col)
        {
            Position = pos;
            Normal = norm;
            Color = col;
            TextureCoordinate = new Vector2(0,0);
        }

        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(32, VertexElementFormat.Color, VertexElementUsage.Color, 0)
            
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    } 

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

            


            VertexPositionColorNormal[] vertices = new VertexPositionColorNormal[8] {
                new VertexPositionColorNormal(new Vector3(-1, 1, 1), new Vector3(-1, 1, 1), Color.AliceBlue),
                new VertexPositionColorNormal(new Vector3(1, 1, 1), new Vector3(-1, 1, 1), Color.AliceBlue),
                new VertexPositionColorNormal(new Vector3(1, 1, -1), new Vector3(-1, 1, 1), Color.AliceBlue),
                new VertexPositionColorNormal(new Vector3(-1, 1, -1), new Vector3(-1, 1, 1), Color.AliceBlue),

                new VertexPositionColorNormal(new Vector3(-1, -1, 1), new Vector3(-1, 1, 1), Color.AliceBlue),
                new VertexPositionColorNormal(new Vector3(1, -1, 1), new Vector3(-1, 1, 1), Color.AliceBlue),
                new VertexPositionColorNormal(new Vector3(1, -1, -1), new Vector3(-1, 1, 1), Color.AliceBlue),
                new VertexPositionColorNormal(new Vector3(-1, -1, -1), new Vector3(-1, 1, 1), Color.AliceBlue)
            };
            //vertices[1].Position = new Vector3(1, 1, 1);
            //vertices[2].Position = new Vector3(1, 1, -1);
            //vertices[3].Position = new Vector3(-1, 1, -1);

            //vertices[4].Position = new Vector3(-1, -1, 1);
            //vertices[5].Position = new Vector3(1, -1, 1);
            //vertices[6].Position = new Vector3(1, -1, -1);
            //vertices[7].Position = new Vector3(-1, -1, -1);

            

            //vertices = null;

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

            //this.IndexBuffer = new IndexBuffer(device, 36 * sizeof(ushort), BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            this.VertexDeclaration = new VertexDeclaration(VertexPositionColorNormal.VertexDeclaration.GetVertexElements());
            this.VertexBuffer = new VertexBuffer(device, typeof(VertexPositionColorNormal), vertices.Length, BufferUsage.WriteOnly);
            this.IndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, 36 * sizeof(ushort), BufferUsage.WriteOnly);
            this.VertexBuffer.SetData(vertices, 0, 8);


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

          
            Device.SetVertexBuffer(this.VertexBuffer);
            this.Device.Indices = this.IndexBuffer;

            Device.BlendState = BlendState.Opaque;
            Device.DepthStencilState = DepthStencilState.Default;

            this.Device.Textures[0] = this.Environment;

            this.Shader.Parameters["ViewProjection"].SetValue(viewProjection);


            //this.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 8, 0, 12);
            Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 8, 0, 12);

            Device.DepthStencilState = DepthStencilState.DepthRead;

            Device.RasterizerState = RasterizerState.CullCounterClockwise;
        }
    }
}
