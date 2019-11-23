using System;
using DinoOtter;

namespace RuntimeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var image = new Image("Assets/patricio.png");

            var game = new Game("Super Jogo Legal", image.Width, image.Height);

            var scene = new Scene();

            image.CenterOrigin();
            image.X = game.HalfWidth;
            image.Y = game.HalfHeight;

            scene.AddGraphic(image);

            game.Start(scene);
        }
    }
}