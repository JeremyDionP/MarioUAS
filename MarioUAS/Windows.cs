using System;
using System.Collections.Generic;
using System.IO;
using LearnOpenTK.Common;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace MarioUAS
{
    class Windows : GameWindow
    {
        private Mesh mesh0;
        private Mesh mesh1;
        private Mesh mesh2;
        private Mesh mesh3;
        private Mesh mesh4;
        private Mesh mesh5;

        Dictionary<string, List<Material>> materials_dict = new Dictionary<string, List<Material>>();

        List<Mesh> object3d = new List<Mesh>();

        private Camera _camera;
        private Vector3 _objectPos;

        private Vector2 _lastMousePosition;
        private bool _firstMove;
        private bool postprocessing = false;

        private bool dark = false;
        private Vector3 specularLight = new Vector3(1.0f, 1.0f, 1.0f);
        private Vector3 lightColor = new Vector3(0.05f, 0.05f, 0.05f);
        private Vector3 spotlightColor = new Vector3(0.0f, 0.0f, 0.0f);

        // Light
        List<Light> lights = new List<Light>();

        // Frame Buffers
        int fbo;

        // Shader
        Shader shader;
        Shader screenShader;
        Shader skyboxShader;

        // Quad Screen
        float[] quadVertices = { // vertex attributes for a quad that fills the entire screen in Normalized Device Coordinates.
            // positions   // texCoords
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,

            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        };

        int _vao;
        int _vbo;
        int texColorBuffer;

        // Cubemap
        int cubemap;
        int _vao_cube;
        int _vbo_cube;

        float[] skyboxVertices = {
            // positions          
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f
        };

        public Windows(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        private Matrix4 generateArbRotationMatrix(Vector3 axis, Vector3 center, float degree)
        {
            var rads = MathHelper.DegreesToRadians(degree);

            var secretFormula = new float[4, 4] {
                { (float)Math.Cos(rads) + (float)Math.Pow(axis.X, 2) * (1 - (float)Math.Cos(rads)), axis.X* axis.Y * (1 - (float)Math.Cos(rads)) - axis.Z * (float)Math.Sin(rads),    axis.X * axis.Z * (1 - (float)Math.Cos(rads)) + axis.Y * (float)Math.Sin(rads),   0 },
                { axis.Y * axis.X * (1 - (float)Math.Cos(rads)) + axis.Z * (float)Math.Sin(rads),   (float)Math.Cos(rads) + (float)Math.Pow(axis.Y, 2) * (1 - (float)Math.Cos(rads)), axis.Y * axis.Z * (1 - (float)Math.Cos(rads)) - axis.X * (float)Math.Sin(rads),   0 },
                { axis.Z * axis.X * (1 - (float)Math.Cos(rads)) - axis.Y * (float)Math.Sin(rads),   axis.Z * axis.Y * (1 - (float)Math.Cos(rads)) + axis.X * (float)Math.Sin(rads),   (float)Math.Cos(rads) + (float)Math.Pow(axis.Z, 2) * (1 - (float)Math.Cos(rads)), 0 },
                { 0, 0, 0, 1}
            };
            var secretFormulaMatrix = new Matrix4(
                new Vector4(secretFormula[0, 0], secretFormula[0, 1], secretFormula[0, 2], secretFormula[0, 3]),
                new Vector4(secretFormula[1, 0], secretFormula[1, 1], secretFormula[1, 2], secretFormula[1, 3]),
                new Vector4(secretFormula[2, 0], secretFormula[2, 1], secretFormula[2, 2], secretFormula[2, 3]),
                new Vector4(secretFormula[3, 0], secretFormula[3, 1], secretFormula[3, 2], secretFormula[3, 3])
            );

            return secretFormulaMatrix;
        }

        protected override void OnLoad()
        {
            GL.ClearColor(1f, 1f, 1f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            // Shader
            shader = new Shader("../../../Shaders/shader.vert", "../../../Shaders/lighting.frag");
            shader.Use();

            // Screen Shader
            screenShader = new Shader("../../../Shaders/PostProcessing.vert",  "../../../Shaders/PostProcessing.frag");
            screenShader.Use();
            screenShader.SetInt("screenTexture", 0);

            // Frame Buffers
            GL.GenFramebuffers(1, out fbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            // Add Texture to Frame Buffer
            Console.WriteLine("TexColorBuffer: " + texColorBuffer);
            GL.GenTextures(1, out texColorBuffer);
            GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 800, 600, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texColorBuffer, 0);
            
            // Render Buffer
            int rbo;
            GL.GenRenderbuffers(1, out rbo);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, 800, 600);
            
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Screen Frame Buffer Created");
            }
            else
            {
                Console.WriteLine("Screen Frame Buffer NOT complete");
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // Initialize default material
            InitDefaultMaterial();
            
            // Create Cube Map
            CreateCubeMap();
            skyboxShader = new Shader("../../../Shaders/skybox.vert", "../../../Shaders/skybox.frag");

            // Vertices
            // Inisialiasi VBO
            _vbo_cube = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo_cube);
            GL.BufferData(BufferTarget.ArrayBuffer, skyboxVertices.Length * sizeof(float), skyboxVertices, BufferUsageHint.StaticDraw);

            // Inisialisasi VAO
            _vao_cube = GL.GenVertexArray();
            GL.BindVertexArray(_vao_cube);
            var vertexLocation = skyboxShader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            skyboxShader.Use();
            skyboxShader.SetInt("skybox", 4);

            // Screen Quad
            GL.GenVertexArrays(1, out _vao);
            GL.GenBuffers(1, out _vbo);
            GL.BindVertexArray(_vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
            
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            // Light Position
            lights.Add(new DirectionLight(new Vector3(3.2f, 4.1f, 1.2f), new Vector3(0.05f, 0.05f, 0.05f),
                new Vector3(1f, 1f, 1f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.2f, -1.0f, -0.3f)));
            lights.Add(new PointLight(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.01f, 0.01f, 0.01f),
                new Vector3(1.0f, 0.7f, 0.4f), new Vector3(0.0f, 0.0f, 0.0f), 0.5f, 0.5f, 1.0f));

            // Initialize Mesh here
            mesh0 = LoadObjFile("../../../Resources/Mario.obj");
            mesh0.setupObject(1.0f, 1.0f);
            mesh0.translate(new Vector3(0f, -0.2f, -0.01f));
            object3d.Add(mesh0);

            mesh1 = LoadObjFile("../../../Resources/Peach.obj");
            mesh1.setupObject(1.0f, 1.0f);
            mesh1.translate(new Vector3(0f, -0.2f, 0f));
            object3d.Add(mesh1);

            mesh2 = LoadObjFile("../../../Resources/castle.obj");
            mesh2.setupObject(1.0f, 1.0f);
            mesh2.translate(new Vector3(0f, -0.2f, 0f));
            object3d.Add(mesh2);

            mesh3 = LoadObjFile("../../../Resources/goomba.obj");
            mesh3.setupObject(1.0f, 1.0f);
            mesh3.translate(new Vector3(-0.27f, 0.01f, 0.1f));
            mesh3.scale(2f);
            object3d.Add(mesh3);

            mesh4 = LoadObjFile("../../../Resources/campfire.obj");
            mesh4.setupObject(1.0f, 1.0f);
            mesh4.translate(new Vector3(0.0f, -0.05f, 0f));
            mesh4.scale(3f);
            object3d.Add(mesh4);

            mesh5 = LoadObjFile("../../../Resources/uastoad.obj");
            mesh5.setupObject(1.0f, 1.0f);
            mesh5.scale(2f);
            mesh5.rotate(0f, 180f, 0f);
            mesh5.translate(new Vector3(-0.23f, -0.177f, 1.5f));
            _objectPos = mesh5.getTransform().ExtractTranslation();
            object3d.Add(mesh5);

            var _cameraPosInit = new Vector3(0, 0.5f, 3f);
            _camera = new Camera(_cameraPosInit, Size.X / (float)Size.Y);
            _camera.Fov = 90f;
            _camera.Yaw -= 90f;
            CursorGrabbed = true;

            base.OnLoad();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (GLFW.GetTime() > 0.02)
            {
                GLFW.SetTime(0.0);
            }

            // Set Spotlight
            mesh0.setSpotLight(_camera.Position, _camera.Front, lightColor, spotlightColor, specularLight,
                    1.0f, 0.09f, 0.032f, MathF.Cos(MathHelper.DegreesToRadians(12.5f)), MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
            mesh1.setSpotLight(_camera.Position, _camera.Front, lightColor, spotlightColor, specularLight,
                    1.0f, 0.09f, 0.032f, MathF.Cos(MathHelper.DegreesToRadians(12.5f)), MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
            mesh2.setSpotLight(_camera.Position, _camera.Front, lightColor, spotlightColor, specularLight,
                    1.0f, 0.09f, 0.032f, MathF.Cos(MathHelper.DegreesToRadians(12.5f)), MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
            mesh3.setSpotLight(_camera.Position, _camera.Front, lightColor, spotlightColor, specularLight,
                    1.0f, 0.09f, 0.032f, MathF.Cos(MathHelper.DegreesToRadians(12.5f)), MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
            mesh4.setSpotLight(_camera.Position, _camera.Front, lightColor, spotlightColor, specularLight,
                    1.0f, 0.09f, 0.032f, MathF.Cos(MathHelper.DegreesToRadians(12.5f)), MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
            mesh5.setSpotLight(_camera.Position, _camera.Front, lightColor, spotlightColor, specularLight,
                    1.0f, 0.09f, 0.032f, MathF.Cos(MathHelper.DegreesToRadians(12.5f)), MathF.Cos(MathHelper.DegreesToRadians(25.5f)));

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            if (postprocessing)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
                GL.Enable(EnableCap.DepthTest);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                // Textured Rendering
                GL.ActiveTexture(TextureUnit.Texture0);

                shader.Use();
                for (int i = 0; i < lights.Count; i++)
                {
                    mesh0.calculateTextureRender(_camera, lights[i], i);
                    mesh1.calculateTextureRender(_camera, lights[i], i);
                    mesh2.calculateTextureRender(_camera, lights[i], i);
                    mesh3.calculateTextureRender(_camera, lights[i], i);
                    mesh4.calculateTextureRender(_camera, lights[i], i);
                    mesh5.calculateTextureRender(_camera, lights[i], i);
                }

                GL.BindVertexArray(0);

                // Default FrameBuffer
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.Disable(EnableCap.DepthTest);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                screenShader.Use();
                screenShader.SetInt("screenTexture", texColorBuffer);

                GL.BindVertexArray(_vao);
                GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }
            else
            {
                shader.Use();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.Enable(EnableCap.DepthTest);
                
                // Textured Rendering
                GL.ActiveTexture(TextureUnit.Texture0);

                for (int i = 0; i < lights.Count; i++)
                {
                    mesh0.calculateTextureRender(_camera, lights[i], i);
                    mesh1.calculateTextureRender(_camera, lights[i], i);
                    mesh2.calculateTextureRender(_camera, lights[i], i);
                    mesh3.calculateTextureRender(_camera, lights[i], i);
                    mesh4.calculateTextureRender(_camera, lights[i], i);
                    mesh5.calculateTextureRender(_camera, lights[i], i);
                }

                // Render Skybox
                GL.DepthFunc(DepthFunction.Lequal);
                // GL.DepthMask(false);
                skyboxShader.Use();
                Matrix4 skyview = _camera.GetViewMatrix().ClearTranslation().ClearScale();
                skyboxShader.SetMatrix4("view", skyview);
                
                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(_camera.Fov),
                    Size.X / (float)Size.Y, 1f, 100f);

                skyboxShader.SetMatrix4("projection", projection);
                skyboxShader.SetInt("skybox", 4);

                GL.BindVertexArray(_vao_cube);
                GL.ActiveTexture(TextureUnit.Texture4);
                GL.BindTexture(TextureTarget.TextureCubeMap, cubemap);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
                GL.DepthFunc(DepthFunction.Less);
            }

            SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            //Console.WriteLine(_camera.Position);

            const float cameraSpeed = 0.5f;

            Vector3 vec = mesh5.getTransform().ExtractTranslation();
            Vector3 vec2 = mesh0.getTransform().ExtractTranslation();
            Vector3 vec3 = mesh1.getTransform().ExtractTranslation();
            Vector3 vec4 = mesh3.getTransform().ExtractTranslation();
            Vector3 vec5 = mesh4.getTransform().ExtractTranslation();

            float x = Math.Abs(vec.X - _camera.Position.X);
            float y = Math.Abs(vec.Y - _camera.Position.Y);
            float z = Math.Abs(vec.Z - _camera.Position.Z);

            float x2 = Math.Abs(vec2.X - _camera.Position.X);
            float y2 = Math.Abs(vec2.Y - _camera.Position.Y);
            float z2 = Math.Abs(vec2.Z - _camera.Position.Z);

            float x3 = Math.Abs(vec3.X - _camera.Position.X);
            float y3 = Math.Abs(vec3.Y - _camera.Position.Y);
            float z3 = Math.Abs(vec3.Z - _camera.Position.Z);

            float x4 = Math.Abs(vec4.X - _camera.Position.X);
            float y4 = Math.Abs(vec4.Y - _camera.Position.Y);
            float z4 = Math.Abs(vec4.Z - _camera.Position.Z);

            float x5 = Math.Abs(vec5.X - _camera.Position.X);
            float y5 = Math.Abs(vec5.Y - _camera.Position.Y);
            float z5 = Math.Abs(vec5.Z - _camera.Position.Z);

            bool hit = false;

            if (KeyboardState.IsKeyReleased(Keys.F))
            {
                if (dark)
                {
                    dark = false;
                    specularLight = new Vector3(1.0f, 1.0f, 1.0f);
                    lightColor = new Vector3(0.05f, 0.05f, 0.05f);
                    spotlightColor = new Vector3(0.0f, 0.0f, 0.0f);
                } else
                {
                    dark = true;
                    specularLight = new Vector3(0.0f, 0.0f, 0.0f);
                    lightColor = new Vector3(0.0f, 0.0f, 0.0f);
                    spotlightColor = new Vector3(1.0f, 1.0f, 1.0f);
                }
            }

            // Escape keyboard
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            // Zoom in
            if (KeyboardState.IsKeyDown(Keys.I))
            {
                _camera.Fov -= 0.5f;
            }

            // Zoom out
            if (KeyboardState.IsKeyDown(Keys.O))
            {
                _camera.Fov += 0.5f;
            }

            if (KeyboardState.IsKeyDown(Keys.W))
            {
                // === COLLISION ===
                // Toad
                if (x <= 0.2 && y <= 0.35 && z <= 0.2)
                {
                    hit = true;
                }
                // Mario
                if ((x2 <= 0.91 && y2 <= 0.6 && z2 <= 0.8 && (_camera.Position.Z < -0.7 && _camera.Position.Z > -0.88 && ((_camera.Position.X < 0.34 && _camera.Position.X > 0.2) || (_camera.Position.X > 0.65 && _camera.Position.X < 0.95)))) || (x2 <= 0.96 && y2 <= 0.6 && z2 <= 1.3 && (_camera.Position.Z < -0.881 && _camera.Position.Z > -2 && _camera.Position.X > 0.34)))
                {
                    hit = true;
                }
                // Peach
                if ((x3 <= 0.32 && y3 <= 0.6 && z3 <= 0.035) || (x3 <= 0.32 && y3 <= 0.45 && z3 <= 0.47 && (_camera.Position.Z < -0.2 && _camera.Position.Z > -0.55)))
                {
                    hit = true;
                }
                // Goomba
                if (x4 <= 0.2 && y4 <= 0.3 && z4 <= 0.2)
                {
                    hit = true;
                }
                // Campfire
                if (x5 <= 0.19 && y5 <= 0.1 && z5 <= 0.15)
                {
                    hit = true;
                }
                // Tembok depan
                if ((_camera.Position.X >= 0.03 && _camera.Position.X <= 0.89) && (_camera.Position.Z >= 0.55 && _camera.Position.Z <= 1.12) && (_camera.Position.Y >= 0.05 && _camera.Position.Y <= 0.51))
                {
                    hit = true;
                }
                // Tembok kanan
                if ((_camera.Position.X >= 0.79 && _camera.Position.X <= 1.13) && (_camera.Position.Z >= -1.173 && _camera.Position.Z <= 1.01) && (_camera.Position.Y >= -0.023 && _camera.Position.Y <= 0.87))
                {
                    hit = true;
                }
                // Tembok kiri
                if ((_camera.Position.X >= -1.2 && _camera.Position.X <= -0.76) && (_camera.Position.Z >= -1.16 && _camera.Position.Z <= 0.7) && (_camera.Position.Y >= -0.12 && _camera.Position.Y <= 0.52))
                {
                    hit = true;
                }
                // Tembok belakang
                if ((_camera.Position.X >= -0.66 && _camera.Position.X <= 0.74) && (_camera.Position.Z >= -1.49 && _camera.Position.Z <= -1.27) && (_camera.Position.Y >= -0.09 && _camera.Position.Y <= 0.89))
                {
                    hit = true;
                }
                if (!hit)
                {
                    _camera.Position += _camera.Front * cameraSpeed * (float)args.Time;
                }
            }
            if (KeyboardState.IsKeyDown(Keys.S))
            {
                hit = false;
                _camera.Position -= _camera.Front * cameraSpeed * (float)args.Time;
            }
            if (KeyboardState.IsKeyDown(Keys.A))
            {
                hit = false;
                _camera.Position -= _camera.Right * cameraSpeed * (float)args.Time;
            }
            if (KeyboardState.IsKeyDown(Keys.D))
            {
                hit = false;
                _camera.Position += _camera.Right * cameraSpeed * (float)args.Time;
            }

            // Naik (Spasi)
            if (KeyboardState.IsKeyDown(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)args.Time;
            }

            // Turun (Ctrl)
            if (KeyboardState.IsKeyDown(Keys.LeftControl))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)args.Time;
            }

            const float _rotationSpeed = 0.5f;

            // K (atas -> Rotasi sumbu x)
            if (KeyboardState.IsKeyDown(Keys.K))
            {
                _objectPos *= 2;
                var axis = new Vector3(1, 0, 0);
                _camera.Position -= _objectPos;
                _camera.Pitch -= _rotationSpeed;
                _camera.Position = Vector3.Transform(_camera.Position,
                    generateArbRotationMatrix(axis, _objectPos, _rotationSpeed).ExtractRotation());
                _camera.Position += _objectPos;

                _camera._front = -Vector3.Normalize(_camera.Position - _objectPos);
                _objectPos /= 2;
            }

            // M (bawah -> Rotasi sumbu x)
            if (KeyboardState.IsKeyDown(Keys.M))
            {
                _objectPos *= 2;
                var axis = new Vector3(1, 0, 0);
                _camera.Position -= _objectPos;
                _camera.Pitch += _rotationSpeed;
                _camera.Position = Vector3.Transform(_camera.Position,
                    generateArbRotationMatrix(axis, _objectPos, -_rotationSpeed).ExtractRotation());
                _camera.Position += _objectPos;

                _camera._front = -Vector3.Normalize(_camera.Position - _objectPos);
                _objectPos /= 2;
            }

            // N (kiri -> Rotasi sumbu y)
            if (KeyboardState.IsKeyDown(Keys.N))
            {
                _objectPos *= 2;
                var axis = new Vector3(0, 1, 0);
                _camera.Position -= _objectPos;
                _camera.Yaw += _rotationSpeed;
                _camera.Position = Vector3.Transform(_camera.Position,
                    generateArbRotationMatrix(axis, _objectPos, _rotationSpeed).ExtractRotation());
                _camera.Position += _objectPos;

                _camera._front = -Vector3.Normalize(_camera.Position - _objectPos);
                _objectPos /= 2;
            }

            // , (kanan -> Rotasi sumbu y)
            if (KeyboardState.IsKeyDown(Keys.Comma))
            {
                _objectPos *= 2;
                var axis = new Vector3(0, 1, 0);
                _camera.Position -= _objectPos;
                _camera.Yaw -= _rotationSpeed;
                _camera.Position = Vector3.Transform(_camera.Position,
                    generateArbRotationMatrix(axis, _objectPos, -_rotationSpeed).ExtractRotation());
                _camera.Position += _objectPos;

                _camera._front = -Vector3.Normalize(_camera.Position - _objectPos);
                _objectPos /= 2;
            }

            // J (putar -> Rotasi sumbu z)
            if (KeyboardState.IsKeyDown(Keys.J))
            {
                _objectPos *= 2;
                var axis = new Vector3(0, 0, 1);
                _camera.Position -= _objectPos;
                _camera.Position = Vector3.Transform(_camera.Position,
                    generateArbRotationMatrix(axis, mesh5.getTransform().ExtractTranslation(), _rotationSpeed).ExtractRotation());
                _camera.Position += _objectPos;

                _camera._front = -Vector3.Normalize(_camera.Position - _objectPos);
                _objectPos /= 2;
            }

            // L (putar -> Rotasi sumbu z)
            if (KeyboardState.IsKeyDown(Keys.L))
            {
                _objectPos *= 2;
                var axis = new Vector3(0, 0, 1);
                _camera.Position -= _objectPos;
                _camera.Position = Vector3.Transform(_camera.Position,
                    generateArbRotationMatrix(axis, mesh5.getTransform().ExtractTranslation(), -_rotationSpeed).ExtractRotation());
                _camera.Position += _objectPos;

                _camera._front = -Vector3.Normalize(_camera.Position - _objectPos);
                _objectPos /= 2;
            }

            if (!IsFocused)
            {
                return;
            }

            const float sensitivity = 0.2f;
            if (_firstMove)
            {
                _lastMousePosition = new Vector2(MouseState.X, MouseState.Y);
                _firstMove = false;
            }
            else
            {
                // Hitung selisih mouse position
                var deltaX = MouseState.X - _lastMousePosition.X;
                var deltaY = MouseState.Y - _lastMousePosition.Y;
                _lastMousePosition = new Vector2(MouseState.X, MouseState.Y);

                _camera.Yaw += deltaX * sensitivity;
                _camera.Pitch -= deltaY * sensitivity;
            }

            base.OnUpdateFrame(args);
        }

        private void InitDefaultMaterial()
        {
            List<Material> materials = new List<Material>();
            Texture diffuseMap = Texture.LoadFromFile("../../../Resources/white.jpg");
            Texture textureMap = Texture.LoadFromFile("../../../Resources/white.jpg");
            Texture dmap = Texture.LoadFromFile("../../../Resources/white.jpg");
            materials.Add(new Material("Default", 128.0f, new Vector3(0.1f), new Vector3(1f), new Vector3(1f),
                    1.0f, diffuseMap, textureMap, dmap));

            materials_dict.Add("Default", materials);
        }

        public Mesh LoadObjFile(string path, bool usemtl = true)
        {
            Mesh mesh = new Mesh("../../../Shaders/shader.vert", "../../../Shaders/lighting.frag");

            List<Vector3> temp_vertices = new List<Vector3>();
            List<Vector3> temp_normals = new List<Vector3>();
            List<Vector3> temp_textureVertices = new List<Vector3>();

            List<uint> temp_vertexIndices = new List<uint>();
            List<uint> temp_normalsIndices = new List<uint>();
            List<uint> temp_textureIndices = new List<uint>();

            List<string> temp_name = new List<string>();
            List<string> temp_materialsName = new List<string>();

            string current_materialsName = "";
            string material_library = "";

            int mesh_count = 0;
            int mesh_created = 0;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Unable to open \"" + path + "\", does not exist.");
            }

            using (StreamReader streamReader = new StreamReader(path))
            {
                while (!streamReader.EndOfStream)
                {
                    List<string> words = new List<string>(streamReader.ReadLine().Split(' '));
                    words.RemoveAll(s => s == string.Empty);

                    if (words.Count == 0)
                        continue;
                    string type = words[0];

                    words.RemoveAt(0);

                    switch (type)
                    {
                        // Render tergantung nama dan objek apa sehingga bisa buat hirarki
                        case "o":
                            if(mesh_count > 0)
                            {
                                Mesh mesh_tmp = new Mesh();

                                // Attach Shader
                                mesh_tmp.setShader(shader);
                                mesh_tmp.setDepthShader(skyboxShader);

                                for (int i = 0; i < temp_vertexIndices.Count; i++)
                                {
                                    uint vertexIndex = temp_vertexIndices[i];
                                    mesh_tmp.AddVertices(temp_vertices[(int)vertexIndex - 1]);
                                }
                                for (int i = 0; i < temp_textureIndices.Count; i++)
                                {
                                    uint textureIndex = temp_textureIndices[i];
                                    mesh_tmp.AddTextureVertices(temp_textureVertices[(int)textureIndex - 1]);
                                }
                                for (int i = 0; i < temp_normalsIndices.Count; i++)
                                {
                                    uint normalIndex = temp_normalsIndices[i];
                                    mesh_tmp.AddNormals(temp_normals[(int)normalIndex - 1]);
                                }
                                mesh_tmp.setName(temp_name[mesh_created]);

                                // Material
                                if(usemtl)
                                {
                                    
                                    List<Material> mtl = materials_dict[material_library];
                                    for (int i = 0; i < mtl.Count; i++)
                                    {
                                        if (mtl[i].Name == current_materialsName)
                                        {
                                            mesh_tmp.setMaterial(mtl[i]);
                                        }
                                    }
                                }
                                else
                                {
                                    List<Material> mtl = materials_dict["Default"];
                                    for (int i = 0; i < mtl.Count; i++)
                                    {
                                        if (mtl[i].Name == "Default")
                                        {
                                            mesh_tmp.setMaterial(mtl[i]);
                                        }
                                    }
                                }
                                
                                if(mesh_count == 1)
                                {
                                    mesh = mesh_tmp;
                                }
                                else
                                {
                                    mesh.child.Add(mesh_tmp);
                                }

                                mesh_created++;
                            }
                            temp_name.Add(words[0]);
                            mesh_count++;
                            break;
                        case "v":
                            temp_vertices.Add(new Vector3(float.Parse(words[0]) / 10, float.Parse(words[1]) / 10, float.Parse(words[2]) / 10));
                            break;
                        case "vt":
                            temp_textureVertices.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), words.Count < 3 ? 0 : float.Parse(words[2])));
                            break;
                        case "vn":
                            temp_normals.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "mtllib":
                            if(usemtl)
                            {
                                string resourceName = "../../../Resources/" + words[0];
                                string nameWOExt = words[0].Split(".")[0];
                                Console.WriteLine(nameWOExt);
                                materials_dict.Add(nameWOExt, LoadMtlFile(resourceName));
                                material_library = nameWOExt;
                            }
                            break;
                        case "usemtl":
                            if(usemtl)
                            {
                                current_materialsName = words[0];
                            }
                            break;
                        // face
                        case "f":
                            foreach (string w in words)
                            {
                                if (w.Length == 0)
                                    continue;

                                string[] comps = w.Split('/');
                                for (int i = 0; i < comps.Length; i++)
                                {
                                    if (i == 0)
                                    {
                                        if (comps[0].Length > 0)
                                        {
                                            temp_vertexIndices.Add(uint.Parse(comps[0]));
                                        }

                                    }
                                    else if (i == 1)
                                    {
                                        if (comps[1].Length > 0)
                                        {
                                            temp_textureIndices.Add(uint.Parse(comps[1]));
                                        }

                                    }
                                    else if (i == 2)
                                    {
                                        if (comps[2].Length > 0)
                                        {
                                            temp_normalsIndices.Add(uint.Parse(comps[2]));
                                        }

                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            if (mesh_created < mesh_count)
            {

                Mesh mesh_tmp = new Mesh();

                // Attach Shader
                mesh_tmp.setShader(shader);
                mesh_tmp.setDepthShader(skyboxShader);

                for (int i = 0; i < temp_vertexIndices.Count; i++)
                {
                    uint vertexIndex = temp_vertexIndices[i];
                    mesh_tmp.AddVertices(temp_vertices[(int)vertexIndex - 1]);
                }
                for (int i = 0; i < temp_textureIndices.Count; i++)
                {
                    uint textureIndex = temp_textureIndices[i];
                    mesh_tmp.AddTextureVertices(temp_textureVertices[(int)textureIndex - 1]);
                }
                for (int i = 0; i < temp_normalsIndices.Count; i++)
                {
                    uint normalIndex = temp_normalsIndices[i];
                    mesh_tmp.AddNormals(temp_normals[(int)normalIndex - 1]);
                }
                mesh_tmp.setName(temp_name[mesh_created]);

                // Material
                if (usemtl)
                {

                    List<Material> mtl = materials_dict[material_library];
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        if (mtl[i].Name == current_materialsName)
                        {
                            mesh_tmp.setMaterial(mtl[i]);
                        }
                    }
                }
                else
                {
                    List<Material> mtl = materials_dict["Default"];
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        if (mtl[i].Name == "Default")
                        {
                            mesh_tmp.setMaterial(mtl[i]);
                        }
                    }
                }

                if (mesh_count == 1)
                {
                    mesh = mesh_tmp;
                }
                else
                {
                    mesh.child.Add(mesh_tmp);
                }

                mesh_created++;
            }
            return mesh;
        }
        
        public List<Material> LoadMtlFile(string path)
        {
            Console.WriteLine("Load MTL file");
            List <Material> materials = new List<Material>();

            List<string> name = new List<string>();

            List<float> shininess = new List<float>();

            List<Vector3> ambient = new List<Vector3>();
            List<Vector3> diffuse = new List<Vector3>();
            List<Vector3> specular = new List<Vector3>();

            List<float> alpha = new List<float>();

            List<string> map_kd = new List<string>();
            List<string> map_ka = new List<string>();
            List<string> map_d = new List<string>();

            // komputer ngecek, apakah file bisa diopen atau tidak
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Unable to open \"" + path + "\", does not exist.");
            }
            // lanjut ke sini
            using (StreamReader streamReader = new StreamReader(path))
            {
                while (!streamReader.EndOfStream)
                {
                    List<string> words = new List<string>(streamReader.ReadLine().Split(' '));
                    words.RemoveAll(s => s == string.Empty);

                    if (words.Count == 0)
                        continue;

                    string type = words[0];

                    words.RemoveAt(0);
                    switch (type)
                    {
                        case "newmtl":
                            if(map_kd.Count < name.Count)
                            {
                                map_kd.Add("white.jpg");
                            }
                            if(map_ka.Count < name.Count)
                            {
                                map_ka.Add("white.jpg");
                            }
                            if (map_d.Count < name.Count)
                            {
                                map_d.Add("white.jpg");
                            }
                            name.Add(words[0]);
                            break;
                        // Shininess
                        case "Ns":
                            shininess.Add(float.Parse(words[0]));
                            break;
                        case "Ka":
                            ambient.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "Kd":
                            diffuse.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "Ks":
                            specular.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "d":
                            alpha.Add(float.Parse(words[0]));
                            break;
                        case "map_Kd":
                            map_kd.Add(words[0]);
                            break;
                        case "map_Ka":
                            map_ka.Add(words[0]);
                            break;
                        default:
                            break;
                    }
                }
            }

            if (map_kd.Count < name.Count)
            {
                map_kd.Add("white.jpg");
            }
            if (map_ka.Count < name.Count)
            {
                map_ka.Add("white.jpg");
            }
            if (map_d.Count < name.Count)
            {
                map_d.Add("white.jpg");
            }

            Dictionary<string, Texture> texture_map_Kd = new Dictionary<string, Texture>();
            for(int i = 0; i < map_kd.Count; i++)
            {
                if(!texture_map_Kd.ContainsKey(map_kd[i]))
                {
                    Console.WriteLine("List of map_Kd key: " + map_kd[i]);
                    texture_map_Kd.Add(map_kd[i],
                        Texture.LoadFromFile("../../../Resources/" + map_kd[i]));
                }
            }

            Dictionary<string, Texture> texture_map_Ka = new Dictionary<string, Texture>();
            for (int i = 0; i < map_ka.Count; i++)
            {
                if (!texture_map_Ka.ContainsKey(map_ka[i]))
                {
                    texture_map_Ka.Add(map_ka[i],
                        Texture.LoadFromFile("../../../Resources/" + map_ka[i]));
                }
            }

            Dictionary<string, Texture> texture_map_d = new Dictionary<string, Texture>();
            for (int i = 0; i < map_d.Count; i++)
            {
                if (!texture_map_d.ContainsKey(map_ka[i]))
                {
                    texture_map_d.Add(map_d[i],
                        Texture.LoadFromFile("../../../Resources/" + map_d[i]));
                }
            }

            for (int i = 0; i < name.Count; i++)
            {
                materials.Add(new Material(name[i], shininess[i], ambient[i], diffuse[i], specular[i], 
                    alpha[i], texture_map_Kd[map_kd[i]], texture_map_Ka[map_ka[i]], texture_map_d[map_d[i]]));
            }

            return materials;
        }

        // CubeMap
        public void CreateCubeMap()
        {
            string[] skyboxPath =
            {
                "../../../Resources/Skybox/sky-7.png",
                "../../../Resources/Skybox/sky-7.png",
                "../../../Resources/Skybox/sky-7.png",
                "../../../Resources/Skybox/sky-7.png",
                "../../../Resources/Skybox/sky-7.png",
                "../../../Resources/Skybox/sky-7.png",
            };
            GL.GenTextures(1, out cubemap);
            GL.BindTexture(TextureTarget.TextureCubeMap, cubemap);

            Console.WriteLine("Cubemap: " + cubemap);
            for (int i = 0; i < skyboxPath.Length; i++)
            {
                using (var image = new Bitmap(skyboxPath[i]))
                {
                    Console.WriteLine(skyboxPath[i] + " LOADED");
                    
                    var data = image.LockBits(
                        new Rectangle(0, 0, image.Width, image.Height),
                        ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i,
                        0,
                        PixelInternalFormat.Rgb,
                        1280,
                        1280,
                        0,
                        PixelFormat.Bgra,
                        PixelType.UnsignedByte,
                        data.Scan0);
                }
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            }
        }
    }
}
