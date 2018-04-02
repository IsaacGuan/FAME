using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Tao.OpenGl;
using System.IO;

namespace FameBase
{
    public class Shader
    {
        // ctrs
        public Shader()
        {

        }

        // ctrs
        public Shader(string vshaderfile, string fshaderfile)
        {
            this.Load(vshaderfile, fshaderfile);
        }

        // create vertex and pixel shader from file, compile and link them
        public void Load(string vshaderfile, string fshaderfile)
        {
            this.CreateAndCompileVertexShader(vshaderfile);
            this.CreateAndCompilePixelShader(fshaderfile);
        }


        private int vertexShader;
        private int fragmentShader;
        private int shaderProgram;

        public void CreateAndCompileVertexShader(string file)
        {
            // Create and compile the vertex shader
            using (StreamReader sr = new StreamReader(file))
            {
                // 1. read vertex shader file
                string[] shaderSource;
                shaderSource = sr.ReadToEnd().Split('\n');

                // 2 create the vertex shader
                this.vertexShader = Gl.glCreateShader(Gl.GL_VERTEX_SHADER);
                Gl.glShaderSource(vertexShader, 1, shaderSource, new IntPtr());
                Gl.glCompileShader(vertexShader);

                if (!this.CompileSuccessful(this.vertexShader))
                {
                    Console.WriteLine("glsl vertex shader compile failed");
                }
            }
        }

        public void CreateAndCompilePixelShader(string file)
        {
            // Create and compile the vertex shader
            // 1. read vertex shader file
            string[] shaderSource;
            using (StreamReader sr = new StreamReader(file))
            {
                shaderSource = sr.ReadToEnd().Split('\n');
            }

            // 2 create the vertex shader
            this.fragmentShader = Gl.glCreateShader(Gl.GL_FRAGMENT_SHADER);
            Gl.glShaderSource(fragmentShader, 1, shaderSource, new IntPtr());
            Gl.glCompileShader(fragmentShader);

            if (!this.CompileSuccessful(this.fragmentShader))
            {
                Console.WriteLine("glsl fragment shader compile failed");
            }

        }

        public void Link()
        {
            // Link the vertex and fragment shader into a shader program
            this.shaderProgram = Gl.glCreateProgram();
            Gl.glAttachShader(shaderProgram, vertexShader);
            Gl.glAttachShader(shaderProgram, fragmentShader);
            Gl.glLinkProgram(shaderProgram);

            if (!this.LinkSuccessful(this.shaderProgram))
            {
                //Console.WriteLine("glsl shader link failed");
            }
        }

        public void Begin()
        {
            Gl.glUseProgram(this.shaderProgram);
        }

        public void End()
        {
            Gl.glUseProgram(0);
        }


        private bool CompileSuccessful(int shader)
        {
            int status;
            Gl.glGetShaderiv(shader, Gl.GL_COMPILE_STATUS, out status);
            return status == Gl.GL_TRUE;
        }

        private bool LinkSuccessful(int program)
        {
            int status;
            Gl.glGetProgramiv(program, Gl.GL_LINK_STATUS, out status);
            return status == Gl.GL_TRUE;
        }

        // check error
        private int CheckGLError()
        {
            int retCode = 0;

            int glErr = Gl.glGetError();
            while (glErr != Gl.GL_NO_ERROR)
            {
                string sError = Glu.gluErrorString(glErr);

                if (sError != null)
                    Console.WriteLine("GL Error #" + glErr + "(" + sError + ") " + " in File " + " at line: ");
                else
                    Console.WriteLine("GL Error #" + glErr + "( no message available ) " + " in File " + " at line: ");

                retCode = 1;
                glErr = Gl.glGetError();
            }
            return retCode;
        }

    }
}
