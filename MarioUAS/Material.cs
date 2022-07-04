using LearnOpenTK.Common;
using OpenTK.Mathematics;
using System;


namespace MarioUAS
{
    class Material
    {
        string name;
        float shininess;
        Vector3 ambient;
        Vector3 diffuse;
        Vector3 specular;
        float alpha;

        Texture map_Kd;
        Texture map_Ka;
        Texture map_d;

        public Material(string name, float shininess, Vector3 ambient, Vector3 diffuse, Vector3 specular,
            float alpha, Texture map_Kd, Texture map_Ka, Texture map_d)
        {
            this.name = name;
            this.shininess = shininess;
            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
            this.alpha = alpha;
            this.map_Kd = map_Kd;
            this.map_Ka = map_Ka;
            this.map_d = map_d;
        }

        public string Name { get => name; set => name = value; }
        public float Shininess { get => shininess; set => shininess = value; }
        public Vector3 Ambient { get => ambient; set => ambient = value; }
        public Vector3 Diffuse { get => diffuse; set => diffuse = value; }
        public Vector3 Specular { get => specular; set => specular = value; }
        public float Alpha { get => alpha; set => alpha = value; }
        public Texture Map_Kd { get => map_Kd; set => map_Kd = value; }
        public Texture Map_Ka { get => map_Ka; set => map_Ka = value; }
        public Texture Map_d { get => map_d; set => map_d = value; }
        public void DisplayAttribute()
        {
            Console.WriteLine("Name: " + name);
            Console.WriteLine("Shininess: " + shininess);
            Console.WriteLine("Ambient " + ambient);
            Console.WriteLine("Diffuse: " + diffuse);
            Console.WriteLine("Specular: " + specular);
            Console.WriteLine("Alpha: " + alpha);
        }
    }
}
