using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolyNesterDemo
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;

    public class UVObjData
    {
        public Vector[] uvs;
        public int[] tris;
        public string target_file;

        // do not modify
        public string file_content;         // the original file contents
        public HashSet<int> replace_lines;  // lines of the file which will be replaced on export

        public UVObjData Clone()
        {
            UVObjData clone = new UVObjData();
            clone.uvs = new Vector[uvs.Length];
            clone.tris = new int[tris.Length];
            clone.replace_lines = new HashSet<int>(replace_lines);


            clone.file_content = file_content;
            clone.target_file = target_file;

            for (int i = 0; i < uvs.Length; i++)
                clone.uvs[i] = uvs[i];

            for (int i = 0; i < tris.Length; i++)
                clone.tris[i] = tris[i];

            return clone;
        }
    }

    static class ObjHandler
    {
        public static UVObjData GetData(string path)
        {
            UVObjData data = new UVObjData();
            data.target_file = path;

            List<int[]> face_list = new List<int[]>();
            List<Vector> uv_list = new List<Vector>();
            List<int> tri_list = new List<int>();
            data.replace_lines = new HashSet<int>(); //point certain lines of the file to corresponding replacement uv indices

            char[] split_id = { ' ' };
            char[] face_split_id = { '/' };

            data.file_content = File.ReadAllText(path);
            int line_counter = 0;
            using (StringReader reader = new StringReader(data.file_content))
            {
                string current_text = reader.ReadLine();

                while (current_text != null)
                {
                    if (!current_text.StartsWith("vt ") && !current_text.StartsWith("f ") && !current_text.StartsWith("v ") && !current_text.StartsWith("vn "))
                    {
                        line_counter++;
                        current_text = reader.ReadLine();
                        continue;
                    }

                    current_text = current_text.Replace("  ", " ");
                    string[] broken = current_text.Split(split_id);

                    if (broken[0] == "vt")
                    {
                        uv_list.Add(new Vector(Convert.ToSingle(broken[1]), Convert.ToSingle(broken[2])));
                        data.replace_lines.Add(line_counter);
                    }
                    else if (broken[0] == "f")
                    {
                        int j = 1;
                        List<int> uv_face = new List<int>();
                        while (j < broken.Length && ("" + broken[j]).Length > 0)
                        {
                            int[] temp = new int[3];
                            string[] part = broken[j].Split(face_split_id, 3);    //Separate the face into individual components (vert, uv, normal)
                            temp[0] = Convert.ToInt32(part[0]);
                            if (part.Length > 1)                                  //Some .obj files skip UV and normal
                            {
                                if (part[1] != "")                                    //Some .obj files skip the uv and not the normal
                                {
                                    temp[1] = Convert.ToInt32(part[1]);
                                }
                                temp[2] = Convert.ToInt32(part[2]);
                            }
                            j++;

                            if (temp[1] > 0)
                                uv_face.Add(temp[1] - 1);

                            face_list.Add(temp);
                        }

                        j = 1;
                        while (j + 2 <= uv_face.Count)     //Create triangles out of the face data.  There will generally be more than 1 triangle per face.
                        {
                            tri_list.Add(uv_face[0]);
                            tri_list.Add(uv_face[j]);
                            tri_list.Add(uv_face[j + 1]);

                            j++;
                        }
                    }
                    else
                    {
                        //Debug.LogError("Should not be able to get here...");
                    }

                    current_text = reader.ReadLine();
                    line_counter++;
                }
            }

            data.uvs = uv_list.ToArray();
            data.tris = tri_list.ToArray();
            
            return data;
        }

        public static void SetData(UVObjData data)
        {
            int line_counter = 0;
            StringBuilder new_text = new StringBuilder();

            int used_uvs = 0;

            using (StringReader reader = new StringReader(data.file_content))
            {
                string current_text = reader.ReadLine();

                while (current_text != null)
                {
                    if (!current_text.StartsWith("vt ") || !data.replace_lines.Contains(line_counter))
                    {
                        line_counter++;
                        new_text.AppendLine(current_text);
                        current_text = reader.ReadLine();
                        continue;
                    }

                    Vector uv = data.uvs[used_uvs++];

                    new_text.AppendLine(string.Format("vt {0} {1}", uv.X, uv.Y));

                    current_text = reader.ReadLine();
                    line_counter++;
                }
            }

            File.WriteAllText(data.target_file, new_text.ToString());
        }

    }
}
