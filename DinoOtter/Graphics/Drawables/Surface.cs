﻿using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;

namespace DinoOtter {
    /// <summary>
    /// Graphic that represents a render target.  By default the game uses a master surface to
    /// render the game to the window.  Be aware of graphics card limiations of render textures when
    /// creating surfaces.
    /// </summary>
    public class Surface : Image {

        #region Private Fields

        RenderStates _states;

        float _cameraX, _cameraY, _cameraAngle, _cameraZoom = 1f;

        RectangleShape _fill;

        List<Shader> _shaders = new List<Shader>();

        RenderTexture
            _postProcessA,
            _postProcessB;

        bool _saveNextFrame;
        string _saveNameFramePath;

        #endregion

        #region Public Fields

        /// <summary>
        /// The color that the surface will fill with at the start of each render.
        /// </summary>
        public Color FillColor;

        /// <summary>
        /// Determines if the Surface will automatically clear at the start of the next render cycle.
        /// </summary>
        public bool AutoClear = true;

        /// <summary>
        /// Determines if the Surface will automatically set its camera to the Scene's camera.
        /// </summary>
        public bool UseSceneCamera;

        #endregion

        #region Public Properties

        /// <summary>
        /// The reference to the Game using this Surface (if it is the main Surface the game is rendering to!)
        /// </summary>
        public Game Game { get; internal set; }

        /// <summary>
        /// The camera X for the view of the surface.
        /// Note: For the game's main surface, this is controlled by the active Scene.
        /// </summary>
        public float CameraX {
            set {
                _cameraX = value;
                RefreshView();
            }
            get {
                return _cameraX;
            }
        }

        /// <summary>
        /// The camera Y for the view of the surface.
        /// Note: For the game's main surface, this is controlled by the active Scene.
        /// </summary>
        public float CameraY {
            set {
                _cameraY = value;
                RefreshView();
            }
            get {
                return _cameraY;
            }
        }

        /// <summary>
        /// The camera angle for the view of the surface.
        /// Note: For the game's main surface, this is controlled by the active Scene.
        /// </summary>
        public float CameraAngle {
            set {
                _cameraAngle = value;
                RefreshView();
            }
            get {
                return _cameraAngle;
            }
        }

        /// <summary>
        /// The camera zoom for the view of the surface.
        /// Note: For the game's main surface, this is controlled by the active Scene.
        /// </summary>
        public float CameraZoom {
            set {
                _cameraZoom = value;
                if (_cameraZoom <= 0) { _cameraZoom = 0.0001f; } //dont be divin' by zero ya hear?
                RefreshView();
            }
            get {
                return _cameraZoom;
            }
        }

        public float CameraWidth {
            get {
                return Width / CameraZoom;
            }
        }

        public float CameraHeight {
            get {
                return Height / CameraZoom;
            }
        }

        /// <summary>
        /// The Texture the Surface has rendered to.
        /// </summary>
        public override Texture Texture {
            get {
                return new Texture(RenderTexture.Texture);
            }
        }

        /// <summary>
        /// Convert an X position into the same position but on the Surface.
        /// TODO: Make this work with scale and rotation.
        /// </summary>
        /// <param name="x">The X position in the Scene.</param>
        /// <returns>The X position on the Surface.</returns>
        public float SurfaceX(float x) {
            return x - X + _cameraX;
        }

        /// <summary>
        /// Convert a Y position into the same position but on the Surface.
        /// TODO: Make this work with scale and rotation.
        /// </summary>
        /// <param name="y">The Y position in the Scene.</param>
        /// <returns>The Y position on the Surface.</returns>
        public float SurfaceY(float y) {
            return y - Y + _cameraY;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a Surface with a specified size.
        /// </summary>
        /// <param name="width">The width of the Surface to create.</param>
        /// <param name="height">The height of the Surface to create.</param>
        /// <param name="color">The default fill color of the Surface.</param>
        public Surface(int width, int height, Color color = null) {
            if (width < 0) throw new ArgumentException("Width must be greater than 0.");
            if (height < 0) throw new ArgumentException("Height must be greater than 0.");

            if (color == null) color = Color.None;

            FillColor = color;
            Width = width;
            Height = height;
            RenderTexture = new RenderTexture((uint)Width, (uint)Height);
            TextureRegion = new Rectangle(0, 0, Width, Height);
            ClippingRegion = TextureRegion;
            RenderTexture.Smooth = Texture.DefaultSmooth;

            _fill = new RectangleShape(new Vector2f(Width, Height)); // Using this for weird clearing bugs on some video cards

            Clear();
        }

        /// <summary>
        /// Creates a Surface of a specified size.
        /// </summary>
        /// <param name="width">The width of the Surface to create.</param>
        /// <param name="height">The height of the Surface to create.</param>
        public Surface(int width, int height) : this(width, height, Color.None) { }

        /// <summary>
        /// Creates a Surface of a specified size.
        /// </summary>
        /// <param name="size">The width and height of the Surface to create.</param>
        public Surface(int size) : this(size, size) { }

        /// <summary>
        /// Creates a Surface of a specified size.
        /// </summary>
        /// <param name="size">The width and height of the Surface to create.</param>
        /// <param name="color">The default fill color of the Surface.</param>
        public Surface(int size, Color color) : this(size, size, color) { }

        #endregion

        #region Private Methods

        void UpdateShader() {
            if (_shaders.Count < 2) {
                if (_postProcessA != null) {
                    _postProcessA.Dispose();
                    _postProcessA = null;
                }
                if (_postProcessB != null) {
                    _postProcessB.Dispose();
                    _postProcessB = null;
                }
            }
            else if (_shaders.Count == 2) {
                if (_postProcessA == null) {
                    _postProcessA = new RenderTexture((uint)Width, (uint)Height);
                }
                if (_postProcessB != null) {
                    _postProcessB.Dispose();
                    _postProcessB = null;
                }
            }
            else if (_shaders.Count > 2) {
                if (_postProcessA == null) {
                    _postProcessA = new RenderTexture((uint)Width, (uint)Height);
                }
                if (_postProcessB == null) {
                    _postProcessB = new RenderTexture((uint)Width, (uint)Height);
                }
            }
        }
       
        void RefreshView() {
            View v = new View(new FloatRect(_cameraX, _cameraY, Width, Height));
            
            v.Rotation = -_cameraAngle;
            v.Zoom(1 / _cameraZoom);
            RenderTarget.SetView(v);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add a shader to be drawn on the surface.  If "Shader" is set, the shader list is ignored.
        /// </summary>
        /// <param name="shader">The Shader to add.</param>
        public void AddShader(Shader shader) {
            _shaders.Add(shader);
            UpdateShader();
        }

        /// <summary>
        /// Remove a shader from the surface.
        /// </summary>
        /// <param name="shader">The Shader to remove.</param>
        public void RemoveShader(Shader shader) {
            _shaders.Remove(shader);
            UpdateShader();
        }

        /// <summary>
        /// Remove the top most shader on the list of shaders.
        /// </summary>
        /// <returns>The removed Shader.</returns>
        public Shader PopShader() {
            if (_shaders.Count == 0) return null;

            var shader = _shaders[_shaders.Count - 1];
            RemoveShader(shader);
            return shader;
        }

        /// <summary>
        /// Calls the SFML Display function on the internal render texture.  Should be used before
        /// any sort of rendering, otherwise the texture will be upside down!
        /// </summary>
        public void Display() {
            RenderTexture.Display();
            SetTexture(new Texture(RenderTexture.Texture));
            Update();
            UpdateDrawable();
        }

        /// <summary>
        /// Remove all shaders from the surface.
        /// </summary>
        public void ClearShaders() {
            _shaders.Clear();
            UpdateShader();
        }

        /// <summary>
        /// Replace all shaders with a single shader.  This will be ignored if "Shader" is set.
        /// </summary>
        /// <param name="shader">The Shader to use.</param>
        public void SetShader(Shader shader) {
            _shaders.Clear();
            _shaders.Add(shader);
            UpdateShader();
        }

        /// <summary>
        /// Draws a graphic to this surface.
        /// </summary>
        /// <param name="graphic">The Graphic to draw.</param>
        /// <param name="x">The X position of the Graphic.</param>
        /// <param name="y">The Y position of the Graphic.</param>
        public void Draw(Graphic graphic, float x = 0, float y = 0) {
            Surface tempSurface = DinoOtter.Draw.Target;
            DinoOtter.Draw.SetTarget(this);
            graphic.Render(x, y);
            DinoOtter.Draw.SetTarget(tempSurface);
        }

        /// <summary>
        /// Fills the surface with the specified color.
        /// </summary>
        /// <param name="color">The Color to fill the Surface with.</param>
        public void Fill(Color color) {
            _fill.Size = new Vector2f(Width, Height);
            _fill.FillColor = color.SFMLColor;
            _fill.Position = new Vector2f(CameraX, CameraY);
            RenderTexture.Draw(_fill); // Sometimes after 20-30 frames, game will freeze here?
        }

        /// <summary>
        /// Clears the surface with the fill color.
        /// </summary>
        public void Clear() {
            RenderTexture.Clear(FillColor.SFMLColor);
        }

        /// <summary>
        /// Clears the surface with a specified color.
        /// </summary>
        /// <param name="color">The Color to clear the Surface with.</param>
        public void Clear(Color color) {
            RenderTexture.Clear(color.SFMLColor);
        }

        /// <summary>
        /// Determines the pixel smoothing for the surface.
        /// </summary>
        public override bool Smooth {
            get { return RenderTexture.Smooth; }
            set { RenderTexture.Smooth = value; }
        }

        /// <summary>
        /// Resizes the surface to a new width and height.
        /// </summary>
        /// <param name="width">The new width of the surface.</param>
        /// <param name="height">The new height of the surface.</param>
        public void Resize(int width, int height) {
            if (width < 0) throw new ArgumentException("Width must be greater than 0.");
            if (height < 0) throw new ArgumentException("Height must be greater than 0.");

            if (Width == width && Height == height) return;

            Width = width;
            Height = height;

            RenderTexture.Dispose(); // not sure if needed?
            RenderTexture = new RenderTexture((uint)Width, (uint)Height);
            TextureRegion = new Rectangle(0, 0, Width, Height);
            ClippingRegion = TextureRegion;
            RenderTexture.Smooth = Texture.DefaultSmooth;

            _fill = new RectangleShape(new Vector2f(Width, Height)); // Using this for weird clearing bugs on some video cards

            Clear();
        }

        /// <summary>
        /// Draw the Surface.
        /// </summary>
        /// <param name="x">The X position offset.</param>
        /// <param name="y">The Y position offset.</param>
        public override void Render(float x = 0, float y = 0) {
            Display();

            SFMLDrawable = RenderShaders();

            base.Render(x, y);

            if (_saveNextFrame) {
                _saveNextFrame = false;
                var saveTarget = new RenderTexture((uint)Width, (uint)Height);
                saveTarget.Draw(SFMLDrawable, _states);
                saveTarget.Display();
                saveTarget.Texture.CopyToImage().SaveToFile(_saveNameFramePath);
                saveTarget.Dispose();
            }

            if (AutoClear) Clear();
        }

        /// <summary>
        /// Draw the surface directly to the game window.  This will refresh the view,
        /// and Display the surface, as well as clear it if AutoClear is true.
        /// </summary>
        /// <param name="game">The Game to render to.</param>
        public void DrawToWindow(Game game) {
            RefreshView();

            Display();

            Drawable drawable = RenderShaders();

            game.Window.Draw(drawable, _states);

            if (_saveNextFrame) {
                _saveNextFrame = false;

                var texture = new SFML.Graphics.Texture(game.Window.Size.X, game.Window.Size.Y);
                texture.Update(game.Window);
                var capture = texture.CopyToImage();
                capture.SaveToFile(_saveNameFramePath);
            }

            if (AutoClear) Clear(FillColor);
        }

        /// <summary>
        /// Draw the Surface to the Game window.
        /// </summary>
        public void DrawToWindow() {
            DrawToWindow(Game);
        }

        /// <summary>
        /// Set view of the Surface.
        /// </summary>
        /// <param name="x">The X position of the view.</param>
        /// <param name="y">The Y position of the view.</param>
        /// <param name="angle">The angle of the view.</param>
        /// <param name="zoom">The zoom of the view.</param>
        public void SetView(float x, float y, float angle = 0, float zoom = 1f) {
            _cameraX = x;
            _cameraY = y;
            _cameraAngle = angle;
            _cameraZoom = zoom;
            RefreshView();
        }

        /// <summary>
        /// Returns a texture by getting the current render texture. I don't know if this works right yet.
        /// </summary>
        /// <returns></returns>
        public Texture GetTexture() {
            return new Texture(RenderTexture.Texture);
        }

        /// <summary>
        /// Saves the next completed render to a file. The supported image formats are bmp, png, tga and jpg.
        /// Note that this waits until the end of the game's Render() to actually export, otherwise it will be blank!
        /// </summary>
        /// <param name="path">
        /// The file path to save to. The type of image is deduced from the extension. If left unspecified the
        /// path will be a png file of the current time in the same folder as the executable.
        /// </param>
        public void SaveToFile(string path = "") {
            _saveNextFrame = true;
            if (path == "") {
                path = string.Format("{0:yyyyMMddHHmmssff}.png", DateTime.Now);
            }
            _saveNameFramePath = path;
        }

        /// <summary>
        /// Matches the view of the surface to the same view of a Scene.
        /// </summary>
        /// <param name="scene">The Scene to match the camera with.</param>
        public void CameraScene(Scene scene) {
            SetView(scene.CameraX + X, scene.CameraY + Y, scene.CameraAngle, scene.CameraZoom);
        }

        /// <summary>
        /// Centers the camera of the surface.
        /// </summary>
        /// <param name="x">The X position to be the center of the scene.</param>
        /// <param name="y">The Y position to be the center of the scene.</param>
        public void CenterCamera(float x, float y) {
            CameraX = x - HalfWidth;
            CameraY = y - HalfHeight;
        }

        #endregion

        #region Internal

        internal RenderTexture RenderTexture;

        internal void Draw(Drawable drawable) {
            RenderTarget.Draw(drawable);
        }

        internal void Draw(Vertex[] vertices, RenderStates states) {
            RenderTarget.Draw(vertices, PrimitiveType.Quads, states);
        }

        internal void Draw(Vertex[] vertices, PrimitiveType primitiveType, RenderStates states) {
            RenderTarget.Draw(vertices, primitiveType, states);
        }

        internal void Draw(List<Vertex> vertices, PrimitiveType primitiveType, RenderStates states) {
            Draw(vertices.ToArray(), primitiveType, states);
        }

        internal void Draw(List<Vertex> vertices, RenderStates states) {
            Draw(vertices.ToArray(), states);
        }

        internal void Draw(Texture texture, float x, float y, float originX, float originY, int width, int height, float scaleX, float scaleY, float angle, Color color = null, BlendMode blend = BlendMode.Alpha, Shader shader = null) {
            _states = new RenderStates(Texture.SFMLTexture);

            //states.BlendMode = (SFML.Graphics.BlendMode)Blend;
            _states.BlendMode = SFMLBlendMode(blend);

            if (Shader != null) {
                _states.Shader = Shader.SFMLShader;
            }

            _states.Transform.Translate(x - OriginX, y - OriginY);
            _states.Transform.Rotate(-Angle, OriginX, OriginY);
            _states.Transform.Scale(ScaleX, ScaleY, OriginX, OriginY);

            var v = new VertexArray(PrimitiveType.Quads);

            if (color == null) color = Color.White;

            v.Append(x, y, color, 0, 0);
            v.Append(x + width, y, color, width, 0);
            v.Append(x + width, y + height, color, width, height);
            v.Append(x, y + height, color, 0, height);

            Draw(v, _states);
        }

        internal void Draw(Drawable drawable, RenderStates states) {
            // Sometimes this hangs in the first 30ish frames?
            // I have no clue what causes this.
            RenderTarget.Draw(drawable, states);
        }

        internal RenderTarget RenderTarget {
            get { return RenderTexture; }
        }

        /// <summary>
        /// This goes through all the shaders and applies them between two buffers, and
        /// eventually spits out the final drawable object.
        /// </summary>
        /// <returns></returns>
        Drawable RenderShaders() {
            Drawable drawable = SFMLVertices;

            _states = new RenderStates(RenderTexture.Texture);
            SetTexture(new Texture(RenderTexture.Texture));
            _states.Transform.Translate(X - OriginX, Y - OriginY);
            _states.Transform.Rotate(Angle, OriginX, OriginY);
            _states.Transform.Scale(ScaleX, ScaleY, OriginX, OriginY);
            //states.BlendMode = (SFML.Graphics.BlendMode)Blend;
            _states.BlendMode = SFMLBlendMode(Blend);

            if (Shader != null) {
                _states.Shader = Shader.SFMLShader;
            }
            else {
                if (_shaders.Count == 1) {
                    _states.Shader = _shaders[0].SFMLShader;
                }
                else if (_shaders.Count == 2) {
                    _states = new RenderStates(RenderTexture.Texture);
                    _states.Shader = _shaders[0].SFMLShader;

                    Game.Instance.RenderCount++;
                    _postProcessA.Draw(SFMLVertices, _states);
                    _postProcessA.Display();

                    _states.Shader = _shaders[1].SFMLShader;

                    drawable = new Sprite(_postProcessA.Texture);
                    _states.Transform.Rotate(Angle, OriginX, OriginY);
                    _states.Transform.Translate(new Vector2f(X - OriginX, Y - OriginY));
                    _states.Transform.Scale(ScaleX, ScaleY, OriginX, OriginY);
                }
                else if (_shaders.Count > 2) {
                    _states = new RenderStates(RenderTexture.Texture);
                    RenderTexture nextRt, currentRt;
                    nextRt = _postProcessB;
                    currentRt = _postProcessA;

                    _states.Shader = _shaders[0].SFMLShader;

                    Game.Instance.RenderCount++;
                    _postProcessA.Draw(SFMLVertices, _states);
                    _postProcessA.Display();

                    for (int i = 1; i < _shaders.Count - 1; i++) {
                        _states = RenderStates.Default;
                        _states.Shader = _shaders[i].SFMLShader;

                        Game.Instance.RenderCount++;
                        nextRt.Draw(new Sprite(currentRt.Texture), _states);
                        nextRt.Display();

                        nextRt = nextRt == _postProcessA ? _postProcessB : _postProcessA;
                        currentRt = currentRt == _postProcessA ? _postProcessB : _postProcessA;
                    }

                    drawable = new Sprite(currentRt.Texture);
                    currentRt.Display();
                    _states.Shader = _shaders[_shaders.Count - 1].SFMLShader;
                    _states.Transform.Rotate(Angle, OriginX, OriginY);
                    _states.Transform.Translate(new Vector2f(X - OriginX, Y - OriginY));
                    _states.Transform.Scale(ScaleX, ScaleY, OriginX, OriginY);
                }
            }

            return drawable;
        }

        #endregion

    }
}
