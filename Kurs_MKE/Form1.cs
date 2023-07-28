using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using System.Xml.Linq;
using static Kurs_Proekt.Form1;


namespace Kurs_Proekt
{
    public partial class Form1 : Form
    {
        double k_razr, h_start, dT, totalT; //значения для построения сетки с разрежением

        List<Element> _elements = new List<Element>(); //экземпляр класса, который хранит в себе все элементы

        List<Element> _elements2D = new List<Element>(); //экземпляр класса для хранения элементов для краевых условий

        List<Data> _data = new List<Data>(); //входные данные

        List<Mesh> _meshX = new List<Mesh>(); //вспомогательный экземпляр класса для построения сетки по оси X

        List<Mesh> _meshY = new List<Mesh>(); //вспомогательный экземпляр класса для построения сетки по оси Y

        List<Mesh> _meshZ = new List<Mesh>(); //вспомогательный экземпляр класса для построения сетки по оси Z
        
        List<Element> _elements2D_0 = new List<Element>();
        List<Element> _elements2D_1 = new List<Element>();
        List<Element> _elements2D_2 = new List<Element>();
        List<Element> _elements2D_3 = new List<Element>();
        List<Element> _elements2D_4 = new List<Element>();
        List<Element> _elements2D_5 = new List<Element>();


        List<double> di; //вектор для храения диагональных элементов глобальной матрицы левой части
        List<double> ggl; //вектор для храения внедиагональных элементов глобальной матрицы левой части
        List<double> ggu; //вектор для храения внедиагональных элементов глобальной матрицы левой части
        List<double> b; //вектор для храения элементов вектора левой части
        double[] q0; //вектор для храения начального значения для вектора правой части

        int[] ig, jg; //элементы для хранения портрета матрицы
        int N;

        string savedata = "", path;
        double tetaS2 = 5;
        public Form1()
        {
            InitializeComponent();
        }
        private void MultMV(List<double> x, List<double> res)
        {
            int n = x.Count;

            for (int i = 0; i < n; i++)
            {
                res[i] = di[i] * x[i];

                int begI = ig[i];
                int endI = ig[i + 1];

                for (int igi = begI; igi < endI; igi++)
                {
                    int Jindex = jg[igi];

                    res[i] += ggl[igi] * x[Jindex];
                    res[Jindex] += ggu[igi] * x[i];
                }
            }
        }
        private double GetF(int i, int j, int k, List<Mesh> MeshX, List<Mesh> MeshY, List<Mesh> MeshZ, double t)
        {
            //return MeshX[i].coord_Nodes * MeshX[i].coord_Nodes + 2 * MeshY[j].coord_Nodes * MeshY[j].coord_Nodes + 3 * MeshZ[k].coord_Nodes * MeshZ[k].coord_Nodes;
            //return Math.Exp(t);
            //return 5;

            double buf = _meshX[i].coord_Nodes + _meshY[j].coord_Nodes + _meshZ[k].coord_Nodes;

            return Math.Exp(buf);

            //return 2 * t*t;
        }
        private void GetRealB(double t)
        {
            int inode_global;
            double buf;

            for (int k = 0; k < _meshZ.Count; k++)
                for (int j = 0; j < _meshY.Count; j++)
                    for (int i = 0; i < _meshX.Count; i++)
                    {
                        inode_global = GetGlobalNode(i, j, k, _meshX.Count, _meshY.Count);
                        buf = _meshX[i].coord_Nodes + _meshY[j].coord_Nodes + _meshZ[k].coord_Nodes;

                        b[inode_global] = -3 * Math.Exp(buf);
                    }

            //for (int i = 0; i < b.Count; i++)
            //{
            //    b[i] = Math.Exp(t);
            //    b[i] = 6;
            //}
        }
        private List<Mesh> getS1(List<Mesh> MeshX, List<Mesh> MeshY, List<Mesh> MeshZ, int N_gran, double t)
        {
            List<Mesh> s = new List<Mesh>();

            for (int j = 0; j < MeshY.Count; j++) //учет краевых по Z
                for (int i = 0; i < MeshX.Count; i++)
                {
                    //s.Add(new Mesh() { Node = i + (j + (MeshZ[0].Node) * (MeshY.Count)) * (MeshX.Count), coord_Nodes = MeshX[i].coord_Nodes * MeshY[j].coord_Nodes * MeshZ[0].coord_Nodes });
                    //s.Add(new Mesh() { Node = i + (j + (MeshZ[MeshZ.Count - 1].Node) * (MeshY.Count)) * (MeshX.Count), coord_Nodes = MeshX[i].coord_Nodes * MeshY[j].coord_Nodes * MeshZ[MeshZ.Count - 1].coord_Nodes });

                    if (N_gran != 4) s.Add(new Mesh() { Node = GetGlobalNode(i, j, 0, MeshX.Count, MeshY.Count), coord_Nodes = GetF(i, j, 0, MeshX, MeshY, MeshZ, t) });
                    if (N_gran != 5) s.Add(new Mesh() { Node = GetGlobalNode(i, j, MeshZ.Count - 1, MeshX.Count, MeshY.Count), coord_Nodes = GetF(i, j, MeshZ.Count - 1, MeshX, MeshY, MeshZ, t) });
                }

            for (int k = 0; k < MeshZ.Count; k++) //учет краевых по Y
                for (int i = 0; i < MeshX.Count; i++)
                {
                    int buf1 = GetGlobalNode(i, 0, k, MeshX.Count, MeshY.Count),
                        buf2 = GetGlobalNode(i, MeshY.Count - 1, k, MeshX.Count, MeshY.Count);

                    //s.Add(new Mesh() { Node = buf1, coord_Nodes = MeshX[i].coord_Nodes * MeshY[0].coord_Nodes * MeshZ[k].coord_Nodes });
                    //s.Add(new Mesh() { Node = buf2, coord_Nodes = MeshX[i].coord_Nodes * MeshY[MeshY.Count - 1].coord_Nodes * MeshZ[k].coord_Nodes });

                    if (N_gran != 2) s.Add(new Mesh() { Node = buf1, coord_Nodes = GetF(i, 0, k, MeshX, MeshY, MeshZ, t) });
                    if (N_gran != 3) s.Add(new Mesh() { Node = buf2, coord_Nodes = GetF(i, MeshY.Count - 1, k, MeshX, MeshY, MeshZ, t) });
                }

            for (int k = 0; k < _meshZ.Count; k++) //учет краевых по X
                for (int j = 0; j < _meshY.Count; j++)
                {
                    int buf1 = GetGlobalNode(0, j, k, MeshX.Count, MeshY.Count),
                        buf2 = GetGlobalNode(MeshX.Count - 1, j, k, MeshX.Count, MeshY.Count);

                    //s.Add(new Mesh() { Node = buf1, coord_Nodes = MeshX[0].coord_Nodes * MeshY[j].coord_Nodes * MeshZ[k].coord_Nodes });
                    //s.Add(new Mesh() { Node = buf2, coord_Nodes = MeshX[MeshX.Count - 1].coord_Nodes * MeshY[j].coord_Nodes * MeshZ[k].coord_Nodes });

                    if (N_gran != 0) s.Add(new Mesh() { Node = buf1, coord_Nodes = GetF(0, j, k, MeshX, MeshY, MeshZ, t) });
                    if (N_gran != 1) s.Add(new Mesh() { Node = buf2, coord_Nodes = GetF(MeshX.Count - 1, j, k, MeshX, MeshY, MeshZ, t) });
                }
            return s;
        }
        private void setS1(List<Mesh> Mesh) //реализует учет первых краевых условий
        {
            ggu = new List<double>();
            for (int i = 0; i < ggl.Count; i++)
                ggu.Add(ggl[i]);

            foreach (var S1 in Mesh)
            {
                di[S1.Node] = 1;
                b[S1.Node] = S1.coord_Nodes;

                int begI = ig[S1.Node];
                int endI = ig[S1.Node + 1];

                for (int igi = begI; igi < endI; igi++)
                {
                    ggl[igi] = 0;
                }
                for (int k = 0; k < jg.Length; k++)
                    if (jg[k] == S1.Node)
                        ggu[k] = 0;
            }
        }
        private void setS2(List<Element> elemets2D, double tetaS2, int N_gran) //реализует учет вторых краевых условий
        {
            List<double> b_loc;
            int inode_glob;
            for(int elem = 0; elem < elemets2D.Count; elem++)
            {
                b_loc = new List<double>();
                for(int i = 0; i < 4; i++)
                {
                    b_loc.Add(0);
                    inode_glob = elemets2D[elem].Node_global[i];

                    for (int j = 0; j < 4; j++)
                        b_loc[i] += GetM2DLocal(i, j, elem, N_gran, elemets2D) * tetaS2;

                    b[inode_glob] += b_loc[i];
                }
            }
        }
        private void AddToMatrix(List<List<double>> A_loc, int el_id)//функция для внесения локальных элементов матрицы в глобальную
        {
            List<int> L = _elements[el_id].Node_global;
            int n_loc = _elements[el_id].Node_global.Count;

            for (int i = 0; i < n_loc; i++)
            {
                di[L[i]] += A_loc[i][i];
            }

            for (int i = 0; i < n_loc; i++)
            {
                int temp = ig[L[i]];
                for (int j = 0; j < i; j++)
                {
                    for (int k = temp; k < ig[L[i] + 1]; k++)
                    {
                        if (jg[k] == L[j])
                        {
                            ggl[k] += A_loc[i][j];
                            break;
                        }
                    }
                }
            }
        }
        private void AddToVector(List<double> b_loc, int el_id)//функция для внесения локальных элементов матрицы в глобальную
        {
            List<int> L = _elements[el_id].Node_global;
            int n_loc = _elements[el_id].Node_global.Count;

            for (int i = 0; i < n_loc; i++)
                b[L[i]] += b_loc[i];
        }
        private void Portrait(int N)
        {
            List<SortedSet<int>> map = new List<SortedSet<int>>();

            for (int i = 0; i < N; i++)
            {
                map.Add(new SortedSet<int>());
            }

            for (int elem = 0; elem < _elements.Count; elem++)
                for (int i = 0; i < _elements[elem].Node_global.Count; i++)
                    for (int j = 0; j < _elements[elem].Node_global.Count; j++)
                        if (i > j)
                            map[_elements[elem].Node_global[i]].Add(_elements[elem].Node_global[j]);

            ig = new int[map.Count + 1];

            ig[0] = 0;

            for (int i = 0; i < map.Count; i++)
            {
                ig[i + 1] = ig[i] + map[i].Count;
            }

            jg = new int[ig[ig.Length - 1]];

            for (int i = 0; i < map.Count; i++)
            {
                var jind = map[i].ToArray();
                for (int j = 0; j < jind.Length; j++)
                    jg[ig[i] + j] = jind[j];
            }

            di = new List<double>();
            ggl = new List<double>();
            b = new List<double>();

            for (int i = 0; i < N; i++)
            {
                di.Add(0);
                b.Add(0);
            }

            for (int i = 0; i < jg.Length; i++)
            {
                ggl.Add(0);
            }

        }
        private void BuildMesh()
        {
            if (_elements.Count != 0)
            {
                _elements.Clear();
                _meshX.Clear();
                _meshY.Clear();
                _meshZ.Clear();
            }

            _meshX = calculate_Mesh_xy(_data[0].X);
            _meshY = calculate_Mesh_xy(_data[0].Y);
            for (int i = 0; i < _data.Count; i++)
            {
                _meshZ = calculate_Mesh_z(_data[i].Z, _meshZ);
            }

        }
        private int GetGlobalNode(int i, int j, int k, int NX, int NY)
        {
            return i + (j + k * NY) * NX;
        }
        private void BuildElements()
        {
            for (int k = 0; k < _meshZ.Count - 1; k++)
                for (int j = 0; j < _meshY.Count - 1; j++)
                    for (int i = 0; i < _meshX.Count - 1; i++)
                    {
                        _elements.Add(new Element() { Node_global = new List<int>(), coord_Nodes_In_Elemet = new List<Coord_Node>(), Lambda = 1 });

                        for (int local_num = 0; local_num < 8; local_num++)
                        {
                            _elements[i + (j + k * (_meshY.Count - 1)) * (_meshX.Count - 1)].Node_global.Add(new int());
                            _elements[i + (j + k * (_meshY.Count - 1)) * (_meshX.Count - 1)].Node_global[local_num] = (i + local_num % 2) + ((j + (local_num / 2) % 2) + (k + local_num / 4) * _meshY.Count) * _meshX.Count;

                            _elements[i + (j + k * (_meshY.Count - 1)) * (_meshX.Count - 1)].coord_Nodes_In_Elemet.Add(new Coord_Node { X = _meshX[i + local_num % 2].coord_Nodes, Y = _meshY[j + (local_num / 2) % 2].coord_Nodes, Z = _meshZ[k + local_num / 4].coord_Nodes });
                        }
                        for (int data_numb = 0; data_numb < _data.Count; data_numb++)
                        {
                            if ((_meshZ[k].coord_Nodes + _meshZ[k + 1].coord_Nodes) / 2 < _data[data_numb].Z)
                            {
                                _elements[i + (j + k * (_meshY.Count - 1)) * (_meshX.Count - 1)].Lambda = _data[data_numb].Lambda;
                                _elements[i + (j + k * (_meshY.Count - 1)) * (_meshX.Count - 1)].Sigma = _data[data_numb].Sigma;
                                break;
                            }
                        }
                    }
        }
        private void BuildElemetnts2D(int N_gran, List<Element> elements)
        {
            if (elements.Count != 0)
                elements.Clear();
            //N_gran - номер грани от 0 до 5 0,1 - X, 2,3 - Y, 4,5 - Z
            int ielem, inode;
            switch (N_gran)
            {
                case 0:
                    {
                        for(int k = 0; k < _meshZ.Count - 1; k++)
                            for (int j = 0; j < _meshY.Count - 1; j++)
                            {
                                ielem = GetGlobalNode(j, k, 0, _meshY.Count - 1, _meshZ.Count - 1);

                                elements.Add(new Element() { Node_global = new List<int>(), coord_Nodes_In_Elemet = new List<Coord_Node>(), Lambda = 1 });
                                for (int local_num = 0; local_num < 4; local_num++)
                                {
                                    inode = GetGlobalNode(0, j + mu(local_num), k + nu(local_num), _meshX.Count, _meshY.Count);

                                    elements[ielem].Node_global.Add(inode);
                                    elements[ielem].coord_Nodes_In_Elemet.Add(new Coord_Node { 
                                        X = _meshX[0].coord_Nodes, 
                                        Y = _meshY[j + mu(local_num)].coord_Nodes, 
                                        Z = _meshZ[k + nu(local_num)].coord_Nodes });
                                }
                            }
                        break;
                    }
                case 1:
                    {
                        for (int k = 0; k < _meshZ.Count - 1; k++)
                            for (int j = 0; j < _meshY.Count - 1; j++)
                            {
                                ielem = GetGlobalNode(j, k, 0, _meshY.Count - 1, _meshZ.Count - 1);

                                elements.Add(new Element() { Node_global = new List<int>(), coord_Nodes_In_Elemet = new List<Coord_Node>(), Lambda = 1 });
                                for (int local_num = 0; local_num < 4; local_num++)
                                {
                                    inode = GetGlobalNode(_meshX.Count - 1, j + mu(local_num), k + nu(local_num), _meshX.Count, _meshY.Count);

                                    elements[ielem].Node_global.Add(inode);
                                    elements[ielem].coord_Nodes_In_Elemet.Add(new Coord_Node
                                    {
                                        X = _meshX[_meshX.Count - 1].coord_Nodes,
                                        Y = _meshY[j + mu(local_num)].coord_Nodes,
                                        Z = _meshZ[k + nu(local_num)].coord_Nodes
                                    });
                                }
                            }

                        break;
                    }
                case 2:
                    {
                        for (int k = 0; k < _meshZ.Count - 1; k++)
                            for (int i = 0; i < _meshX.Count - 1; i++)
                            {
                                ielem = GetGlobalNode(i, k, 0, _meshX.Count - 1, _meshZ.Count - 1);

                                elements.Add(new Element() { Node_global = new List<int>(), coord_Nodes_In_Elemet = new List<Coord_Node>(), Lambda = 1 });
                                for (int local_num = 0; local_num < 4; local_num++)
                                {
                                    inode = GetGlobalNode(i + mu(local_num), 0, k + nu(local_num), _meshX.Count, _meshY.Count);

                                    elements[ielem].Node_global.Add(inode);
                                    elements[ielem].coord_Nodes_In_Elemet.Add(new Coord_Node
                                    {
                                        X = _meshX[i + mu(local_num)].coord_Nodes,
                                        Y = _meshY[0].coord_Nodes,
                                        Z = _meshZ[k + nu(local_num)].coord_Nodes
                                    });
                                }
                            }
                        break;
                    }
                case 3:
                    {
                        for (int k = 0; k < _meshZ.Count - 1; k++)
                            for (int i = 0; i < _meshX.Count - 1; i++)
                            {
                                ielem = GetGlobalNode(i, k, 0, _meshX.Count - 1, _meshZ.Count - 1);

                                elements.Add(new Element() { Node_global = new List<int>(), coord_Nodes_In_Elemet = new List<Coord_Node>(), Lambda = 1 });
                                for (int local_num = 0; local_num < 4; local_num++)
                                {
                                    inode = GetGlobalNode(i + mu(local_num), _meshY.Count - 1, k + nu(local_num), _meshX.Count, _meshY.Count);

                                    elements[ielem].Node_global.Add(inode);
                                    elements[ielem].coord_Nodes_In_Elemet.Add(new Coord_Node
                                    {
                                        X = _meshX[i + mu(local_num)].coord_Nodes,
                                        Y = _meshY[_meshY.Count - 1].coord_Nodes,
                                        Z = _meshZ[k + nu(local_num)].coord_Nodes
                                    });
                                }
                            }
                        break;
                    }
                case 4:
                    {
                        for (int j = 0; j < _meshY.Count - 1; j++)
                            for (int i = 0; i < _meshX.Count - 1; i++)
                            {
                                ielem = GetGlobalNode(i, j, 0, _meshX.Count - 1, _meshY.Count - 1);

                                elements.Add(new Element() { Node_global = new List<int>(), coord_Nodes_In_Elemet = new List<Coord_Node>(), Lambda = 1 });
                                for (int local_num = 0; local_num < 4; local_num++)
                                {
                                    inode = GetGlobalNode(i + mu(local_num), j + nu(local_num), 0, _meshX.Count, _meshY.Count);

                                    elements[ielem].Node_global.Add(inode);
                                    elements[ielem].coord_Nodes_In_Elemet.Add(new Coord_Node
                                    {
                                        X = _meshX[i + mu(local_num)].coord_Nodes,
                                        Y = _meshY[j + nu(local_num)].coord_Nodes,
                                        Z = _meshZ[0].coord_Nodes
                                    });
                                }
                            }
                        break;
                    }
                case 5:
                    {
                        for (int j = 0; j < _meshY.Count - 1; j++)
                            for (int i = 0; i < _meshX.Count - 1; i++)
                            {
                                ielem = GetGlobalNode(i, j, 0, _meshX.Count - 1, _meshY.Count - 1);

                                elements.Add(new Element() { Node_global = new List<int>(), coord_Nodes_In_Elemet = new List<Coord_Node>(), Lambda = 1 });
                                for (int local_num = 0; local_num < 4; local_num++)
                                {
                                    inode = GetGlobalNode(i + mu(local_num), j + nu(local_num), _meshZ.Count - 1, _meshX.Count, _meshY.Count);

                                    elements[ielem].Node_global.Add(inode);
                                    elements[ielem].coord_Nodes_In_Elemet.Add(new Coord_Node
                                    {
                                        X = _meshX[i + mu(local_num)].coord_Nodes,
                                        Y = _meshY[j + nu(local_num)].coord_Nodes,
                                        Z = _meshZ[_meshZ.Count - 1].coord_Nodes
                                    });
                                }
                            }
                        break;
                    }
                default:
                    {
                        MessageBox.Show("BuildElemetnts2D: Нет подходящей грани!");
                        break;
                    }
            }
        }
        private void BuildGlobalMatrix()
        {
            for (int elem = 0; elem < _elements.Count; elem++) // сборка глобальной матрицы левой части
            {
                List<List<double>> A_loc = new List<List<double>>();

                for (int i = 0; i < 8; i++)
                {
                    A_loc.Add(new List<double>());
                    for (int j = 0; j < 8; j++)
                    {
                        A_loc[i].Add(GetGLocal(i, j, elem));
                    }
                }
                AddToMatrix(A_loc, elem);
            }

        }
        private void BuildGlobalMatrixOnTimeStep(double dT) //сборка глобальной матрицы на временном шаге
        {
            for (int elem = 0; elem < _elements.Count; elem++) // сборка глобальной матрицы левой части
            {
                List<List<double>> A_loc = new List<List<double>>();

                for (int i = 0; i < 8; i++)
                {
                    A_loc.Add(new List<double>());
                    for (int j = 0; j < 8; j++)
                    {
                        A_loc[i].Add(GetGLocal(i, j, elem) + 1 / dT * GetMLocal(i, j, elem)); 
                    }
                }
                AddToMatrix(A_loc, elem);
            }

        }
        private void BuildVectOnTimeStep(double dT, double T, double [] q)// сборка вектора правой части на временном шаге
        {
            List<double> b_loc;
            int inode_global;
            for (int elem = 0; elem < _elements.Count; elem++)
            { 
                b_loc = new List<double>();

                for (int i = 0; i < 8; i++)
                {
                    inode_global = _elements[elem].Node_global[i];
                    b_loc.Add(0);
                    for (int j = 0; j < 8; j++)
                    {
                        b_loc[i] += 1 / dT * GetMLocal(i, j, elem) * q[inode_global];
                    }
                }
                AddToVector(b_loc, elem);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            string filename = openFileDialog1.FileName;
            try
            {
                _data = readData(filename);
            }
            catch
            {
                MessageBox.Show("ЭКЗЕПШе(о)Н БЛЕАТЬ!!!!!");
                return;
            }
        }
        private List<Mesh> calculate_Mesh_xy(double coord_end)//функция реализует разбиение сетки по осям x и y
        {
            List<Mesh> mesh = new List<Mesh>();

            List<Mesh> plus = new List<Mesh>();
            int i = 0;
            plus.Add(new Mesh { Node = 0, coord_Nodes = 0 });

            while (plus[i].coord_Nodes < coord_end)
            {
                i++;
                plus.Add(new Mesh { Node = i, coord_Nodes = plus[i - 1].coord_Nodes + h_start * Math.Pow(k_razr, i) });

            }
            if ((plus[i - 1].coord_Nodes + plus[i].coord_Nodes) / 2 > coord_end)
            {
                plus[i - 1].coord_Nodes = coord_end;
                plus.RemoveAt(i);
            }
            else
            {
                plus[i].coord_Nodes = coord_end;
            }

            //List<Mesh> minus = new List<Mesh>();
            //minus.Add(new Mesh { Node = 1, coord_Nodes = 0 });

            //i = 0;
            //while (minus[i].coord_Nodes > -1 * coord_end)
            //{
            //    i++;
            //    minus.Add(new Mesh { Node = i, coord_Nodes = minus[i - 1].coord_Nodes - h_start * Math.Pow(k_razr, i) });
            //}
            //if ((minus[i - 1].coord_Nodes + minus[i].coord_Nodes) / 2 < -1 * coord_end)
            //{
            //    minus[i - 1].coord_Nodes = -1 * coord_end;
            //    minus.RemoveAt(i);
            //    i--;
            //}
            //else
            //{
            //    minus[i].coord_Nodes = -1 * coord_end;
            //}
            int node_number = 0;

            //for (int j = i; j > 0; j--)
            //{
            //    mesh.Add(new Mesh { Node = node_number++, coord_Nodes = minus[j].coord_Nodes });
            //}
            for (int j = 0; j <= i; j++)
            {
                mesh.Add(new Mesh { Node = node_number++, coord_Nodes = plus[j].coord_Nodes });
            }
            return mesh;
        }
        private List<Mesh> calculate_Mesh_z(double coord_end, List<Mesh> meshList)
        {
            if (meshList.Count == 0)
            {
                meshList.Add(new Mesh { Node = 0, coord_Nodes = 0 });
            }

            //int i = meshList.Count - 1;
            int i = 0;
           
            while (meshList[i].coord_Nodes < coord_end)
            {
                i++;
                //meshList.Add(new Mesh { Node = i, coord_Nodes = meshList[i - 1].coord_Nodes - h_start * Math.Pow(k_razr, i) });
                meshList.Add(new Mesh { Node = i, coord_Nodes = meshList[i - 1].coord_Nodes + h_start * Math.Pow(k_razr, i) });

            }
            if ((meshList[i - 1].coord_Nodes + meshList[i].coord_Nodes) / 2 > coord_end)
            {
                meshList[i - 1].coord_Nodes = coord_end;
                meshList.RemoveAt(i);
            }
            else
            {
                meshList[i].coord_Nodes = coord_end;
            }

            List<Mesh> res = new List<Mesh>();

            //for (int j = meshList.Count - 1; j >= 0; j--)
            //{
            //    meshList[j].Node = meshList.Count - 1 - j;
            //    res.Add(meshList[j]);
            //}

            for (int j = 0; j < meshList.Count; j++)
            {
                //meshList[j].Node = meshList.Count - 1 - j;
                res.Add(meshList[j]);
            }

            return res;
        }

        //private List<Mesh> calculate_Mesh_z(double coord_end, List<Mesh> meshList)//функция реализует разбиение сетки по оси z
        //{
        //    if (meshList.Count == 0)
        //    {
        //        meshList.Add(new Mesh { Node = 0, coord_Nodes = 0 });
        //    }

        //    int i = meshList.Count - 1;

        //    while (meshList[i].coord_Nodes < coord_end)
        //    {
        //        i++;
        //        meshList.Add(new Mesh { Node = i, coord_Nodes = meshList[i - 1].coord_Nodes + h_start * Math.Pow(k_razr, i - 1) });
        //    }
        //    if ((meshList[i - 1].coord_Nodes + meshList[i].coord_Nodes) / 2 > coord_end)
        //    {
        //        meshList[i - 1].coord_Nodes = coord_end;
        //        meshList.RemoveAt(i);
        //    }
        //    else
        //    {
        //        meshList[i].coord_Nodes = coord_end;
        //    }

        //    return meshList;
        //}
        private int mu(int i)
        {
            return i % 2;
        }
        private int nu(int i)
        {
            return (i / 2) % 2;
        }
        private int teta(int i)
        {
            return i / 4;
        }
        private double[,] G(double h)
        {
            return new double[2, 2] { { 1 / h, -1 / h }, { -1 / h, 1 / h } };
        }
        private double[,] M(double h)
        {
            return new double[2, 2] { { h / 3, h / 6 }, { h / 6, h / 3 } };
        }
        private double GetM2DLocal(int i, int j, int elem2d, int N_gran, List<Element> elements2D)
        {
            double h1, h2, buf;
            double[,] M1, M2;
            if (N_gran == 0 || N_gran == 1)
            {
                h1 = Math.Abs(elements2D[elem2d].coord_Nodes_In_Elemet[0].Y - elements2D[elem2d].coord_Nodes_In_Elemet[1].Y);
                h2 = Math.Abs(elements2D[elem2d].coord_Nodes_In_Elemet[0].Z - elements2D[elem2d].coord_Nodes_In_Elemet[2].Z);
            }
            else if (N_gran == 2 || N_gran == 3)
            {
                h1 = Math.Abs(elements2D[elem2d].coord_Nodes_In_Elemet[0].X - elements2D[elem2d].coord_Nodes_In_Elemet[1].X);
                h2 = Math.Abs(elements2D[elem2d].coord_Nodes_In_Elemet[0].Z - elements2D[elem2d].coord_Nodes_In_Elemet[2].Z);
            }
            else if (N_gran == 4 || N_gran == 5)
            {
                h1 = Math.Abs(elements2D[elem2d].coord_Nodes_In_Elemet[0].X - elements2D[elem2d].coord_Nodes_In_Elemet[1].X);
                h2 = Math.Abs(elements2D[elem2d].coord_Nodes_In_Elemet[0].Y - elements2D[elem2d].coord_Nodes_In_Elemet[2].Y);
            }
            else
            {
                MessageBox.Show("GetM2DLocal: Нет подходящей грани!");
                return 0;
            }
            M1 = M(h1);
            M2 = M(h2);
            buf = M1[mu(i), mu(j)] * M2[nu(i), nu(j)];
            
            return buf;
        }
        private double GetGLocal(int i, int j, int elem) //сборка локальной матрицы жесткости
        {
            double hx = _elements[elem].coord_Nodes_In_Elemet[0].X - _elements[elem].coord_Nodes_In_Elemet[1].X,
                   hy = _elements[elem].coord_Nodes_In_Elemet[0].Y - _elements[elem].coord_Nodes_In_Elemet[2].Y,
                   hz = _elements[elem].coord_Nodes_In_Elemet[0].Z - _elements[elem].coord_Nodes_In_Elemet[4].Z;
            double[,] gX = G(Math.Abs(hx)),
                      gY = G(Math.Abs(hy)),
                      gZ = G(Math.Abs(hz)),
                      mX = M(Math.Abs(hx)),
                      mY = M(Math.Abs(hy)),
                      mZ = M(Math.Abs(hz));

            int mu_i = mu(i), mu_j = mu(j),
                nu_i = nu(i), nu_j = nu(j),
                teta_i = teta(i), teta_j = teta(j);

            double SX = gX[mu_i, mu_j] * mY[nu_i, nu_j] * mZ[teta_i, teta_j],
                   SY = mX[mu_i, mu_j] * gY[nu_i, nu_j] * mZ[teta_i, teta_j],
                   SZ = mX[mu_i, mu_j] * mY[nu_i, nu_j] * gZ[teta_i, teta_j];

            double buf = _elements[elem].Lambda * (SX + SY + SZ);

            return buf;
        }
        private double GetMLocal(int i, int j, int elem) //сборка локальной матрицы массы
        {
            double[,] mX = M(Math.Abs(_elements[elem].coord_Nodes_In_Elemet[0].X - _elements[elem].coord_Nodes_In_Elemet[1].X)),
                      mY = M(Math.Abs(_elements[elem].coord_Nodes_In_Elemet[0].Y - _elements[elem].coord_Nodes_In_Elemet[2].Y)),
                      mZ = M(Math.Abs(_elements[elem].coord_Nodes_In_Elemet[0].Z - _elements[elem].coord_Nodes_In_Elemet[4].Z));
            double buf = _elements[elem].Sigma * mX[mu(i), mu(j)] * mY[nu(i), nu(j)] * mZ[teta(i), teta(j)];
            return buf;
        }
        private double[] ReadQ(string path, string q_name) //чтение q для параболической задачи
        {
            string[] fileText = File.ReadAllLines(path + @"\" + q_name + ".txt");
            
            double[] q = new double[fileText.Length];

            for (int i = 0; i < fileText.Length; i++)
            {
                q[i] = double.Parse(fileText[i]);
            }
                return q;
        }
        private void SaveQ(string path, string q_name, double[] q) //сохранение q для параболической задачи
        {
            string savedata = "";

            for (int i = 0; i < q.Length; i++)
                savedata += q[i].ToString() + "\n";

            File.WriteAllText(path + @"\" + q_name + ".txt", savedata);
        }
        private List<Data> readData(string filename) //реализует чтение данных из файла
        {
            string[] fileText = File.ReadAllLines(filename);

            List<Data> Data = new List<Data>();

            foreach (string data in fileText)
            {
                string[] datas;
                datas = data.Split('\t');
                Data.Add(new Data() { 
                    X = double.Parse(datas[0].Replace(",", "."), CultureInfo.InvariantCulture), 
                    Y = double.Parse(datas[1].Replace(",", "."), CultureInfo.InvariantCulture), 
                    Z = double.Parse(datas[2].Replace(",", "."), CultureInfo.InvariantCulture), 
                    Lambda = double.Parse(datas[3].Replace(",", "."), CultureInfo.InvariantCulture),
                    Sigma = double.Parse(datas[4].Replace(",", "."), CultureInfo.InvariantCulture) });
            }
            return Data;
        }
        private double[] calcQ0() //задание начальных условий для параболической задачи
        {
            int N = _meshX.Count * _meshY.Count * _meshZ.Count;
            double[] q0 = new double[N];
            int inode;

            for (int k = 0; k < _meshZ.Count; k++)
                for (int j = 0; j < _meshY.Count; j++)
                    for (int i = 0; i < _meshX.Count; i++)
                    {
                        inode = GetGlobalNode(i, j, k, _meshX.Count, _meshY.Count);
                        q0[inode] = GetF(i, j, k, _meshX, _meshY, _meshZ, 0);
                    }

            return q0;
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

            if (double.TryParse(this.textBox1.Text, out k_razr))
            {
                textBox1.BackColor = Color.White;
            }
            else
            {
                textBox1.BackColor = Color.Red;
            }
        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
        }
        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
        }
        private void button2_Click(object sender, EventArgs e)
        {
            k_razr = double.Parse(textBox1.Text);
            //tetaS2 = double.Parse(textBox3.Text);
            int N_granS2 = int.Parse(numericUpDown4.Text);

            h_start = double.Parse(textBox3.Text);

            if (_data.Count == 0)
            {
                MessageBox.Show("Отсутствуют данные");
                return;
            }

            BuildMesh();

            BuildElements();

            dataGridView1.DataSource = _meshX;
            dataGridView2.DataSource = _meshY;
            dataGridView3.DataSource = _meshZ;

            N = _meshX.Count * _meshY.Count * _meshZ.Count;

            Portrait(N);

            BuildGlobalMatrix();

            if (_elements2D.Count != 0)
            {
                _elements2D.Clear();
            }

            BuildElemetnts2D(0, _elements2D_0);
            BuildElemetnts2D(1, _elements2D_1);
            BuildElemetnts2D(2, _elements2D_2);
            BuildElemetnts2D(3, _elements2D_3);
            BuildElemetnts2D(4, _elements2D_4);
            BuildElemetnts2D(5, _elements2D_5);

            setS2(_elements2D_0, -1, 0);
            setS2(_elements2D_1, 1, 1);
            setS2(_elements2D_2, -1, 2);
            setS2(_elements2D_3, 1, 3);
            setS2(_elements2D_4, -1, 4);
            setS2(_elements2D_5, 1, 5);
            
            
            setS1(getS1(_meshX, _meshY, _meshZ, -1, 0));

            //b[(_meshX.Count / 2) + (_meshY.Count / 2 + (_meshZ.Count - 1) * (_meshY.Count)) * (_meshX.Count)] = 0; //источник

            SLAE solve = new SLAE(N, di, ig, jg, ggl, ggu, b);

            solve.LOS_LU();

            List<double> res = new List<double>(),
                         x = new List<double>();

            for (int i = 0; i < N; i++)
            {
                res.Add(0);
                x.Add(solve.q[i]);
            }
            MultMV(x, res);

            List<OutRes> outres = new List<OutRes>(),
                         outx = new List<OutRes>(),
                         outb = new List<OutRes>();

            for (int i = 0; i < N; i++)
            {
                outres.Add(new OutRes() { i = i, vol = res[i] });
                outx.Add(new OutRes() { i = i, vol = x[i] });
                outb.Add(new OutRes() { i = i, vol = b[i] });
            }

            //solve.q
            double temp = 0;
            for (int i = 0; i < N; i++)
            {
                temp += Math.Sqrt((res[i] - b[i]) * (res[i] - b[i]));
            }
            //MessageBox.Show("Невязка = " + temp.ToString());

            textBox2.Text = temp.ToString();

            dataGridView4.DataSource = outx;
            dataGridView5.DataSource = outres;
            dataGridView6.DataSource = outb;


            //for (int i = 0; i < _meshX.Count; i++)
            //{
            //    File.AppendAllText(path, i + "\t" + _meshX[i].coord_Nodes.ToString() + "\t" + solve.q[i + ((_meshY.Count / 2) + (_meshZ.Count - 1) * (_meshY.Count)) * (_meshX.Count)].ToString() + "\n");
            //}
            //for (int i = 0; i < _meshX.Count; i++)
            //{
            //    File.AppendAllText(path, _meshX[i].coord_Nodes.ToString() + "\t" + solve.q[_meshX[i].Node + (_meshY.Count / 2 + (_meshZ.Count - 1) * (_meshY.Count)) * (_meshX.Count)].ToString() + "\n");

            //    //savedata += _meshX[i].coord_Nodes.ToString() + "\t" + solve.q[_meshX[i].Node + (_meshY.Count / 2 + (_meshZ.Count - 1) * (_meshY.Count)) * (_meshX.Count)].ToString() + "\n";
            //}


            //string originalText = "Hello Metanit.com";
            // запись строки
            //File.WriteAllTextAsync(path, originalText);
            // дозапись в конец файла


           //textBox2.Text = ((_meshX.Count / 2) + (_meshY.Count / 2 + (_meshZ.Count - 1) * (_meshY.Count)) * (_meshX.Count)).ToString();


            double[] q = solve.q;
            savedata = "";

            for (int k = 0; k < _meshZ.Count; k++)
                for (int j = 0; j < _meshY.Count; j++)
                    for (int i = 0; i < _meshX.Count; i++)
                    {
                        savedata += (i + (j + k * (_meshY.Count)) * (_meshX.Count)).ToString() + "\t" + _meshX[i].coord_Nodes.ToString() + "\t" + _meshY[j].coord_Nodes.ToString() + "\t" + _meshZ[k].coord_Nodes.ToString() + "\t" + q[i + (j + k * (_meshY.Count)) * (_meshX.Count)].ToString() + "\n";
                    }

            //for (int i = 0; i < q.Length; i++)
            //{
            //    savedata += q[i].ToString() + "\n";
            //}

        }
        private void button3_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Title = "Save Data as...";
            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            string filename_save = saveFileDialog1.FileName;

            File.WriteAllText(filename_save, savedata);
        }
        private void button4_Click(object sender, EventArgs e)//расчет по времени
        {
            dT = double.Parse(textBox4.Text);
            totalT = double.Parse(textBox5.Text);
            

            double[] q;

            double time = dT;
            SLAE solve;


            for (int iT = 1; time < totalT; iT++)
            {
                for (int i = 0; i < di.Count; i++)
                {
                    di[i] = 0;
                    b[i] = 0;
                }

                for (int i = 0; i < ggl.Count; i++)
                {
                    ggl[i] = 0;
                    ggu[i] = 0;
                }

                time = dT * iT;

                q = ReadQ(path, "q" + (iT - 1).ToString());
                if (q.Length == 0)
                {
                    MessageBox.Show("Не задано q" + (iT - 1).ToString());
                    return;
                }

                //собрать матрицу
                BuildGlobalMatrixOnTimeStep(dT);
                GetRealB(time);
                //собрать вектор правой части
                BuildVectOnTimeStep(dT, time, q);

                //учесть краевые
                //setS2(_elements2D_0, -1, 0);
                //setS2(_elements2D_1, 1, 1);
                //setS2(_elements2D_2, -1, 2);
                //setS2(_elements2D_3, 1, 3);
                //setS2(_elements2D_4, -1, 4);
                //setS2(_elements2D_5, 1, 5);

                setS1(getS1(_meshX, _meshY, _meshZ, -1, time));


                //решить СЛАУ
                solve = new SLAE(N, di, ig, jg, ggl, ggu, b);
                solve.LOS_LU();

                //сохранить
                SaveQ(path, "q" + iT.ToString(), solve.q);
            }

        }
        private void button5_Click(object sender, EventArgs e)//расчет q0
        {
            if (_data.Count == 0)
            {
                MessageBox.Show("Отсутствуют данные");
                return;
            }

            if (folderBrowserDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            path = folderBrowserDialog1.SelectedPath;

            q0 = calcQ0();

            SaveQ(path, "q0", q0);
        }
        public class Coord_Node //класс для хранения координат
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }
        public class Element //класс описывающий элемент
        {
            public List<int> Node_global { get; set; }
            public List<Coord_Node> coord_Nodes_In_Elemet { get; set; }
            public double Lambda { get; set; }
            public double Sigma { get; set; }
        }
        public class Mesh //класс описывающий одномерную сетку
        {
            public int Node { get; set; }
            public double coord_Nodes { get; set; }
        }
        public class Data //вспомогательный класс для чтения и хранения данных
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double Lambda { get; set; }
            public double Sigma { get; set; }
        }
        public class SLAE //класс для решения СЛАУ
        {
            //public static SplineData SplineData { get; set; }
            public static int N;
            public static int[] ig;
            public static int[] jg;
            public static double[] di;
            public static double[] ggl;
            public static double[] ggu;

            // for Solver
            public static double[] l, u, d;
            public static double[] F, temp, temp0;
            public static double[] r, z, p;

            public double[] q { get; set; }
            // LU факторизация
            static void CalcLU()
            {
                for (int i = 0; i < di.Length; i++)
                    d[i] = di[i];
                for (int i = 0; i < ggl.Length; i++)
                    l[i] = ggl[i];
                for (int i = 0; i < ggu.Length; i++)
                    u[i] = ggu[i];

                double sumU, sumL, sumD;
                int n = N;

                for (int i = 0; i < n; i++)
                {
                    sumD = 0;

                    int begI = ig[i];
                    int endI = ig[i + 1];
                    for (int igi = begI; igi < endI; igi++)
                    {
                        sumU = 0;
                        sumL = 0;

                        int Jindex = jg[igi];

                        for (int igj = begI; igj < igi; igj++)
                        {
                            int begJ = ig[Jindex];
                            int endJ = ig[Jindex + 1];

                            for (int jgi = begJ; jgi < endJ; jgi++)
                            {
                                if (jg[igj] == jg[jgi])
                                {
                                    sumL += l[igj] * u[jgi];
                                    sumU += l[jgi] * u[igj];
                                }
                            }
                        }
                        l[igi] -= sumL;
                        u[igi] -= sumU;
                        u[igi] /= d[Jindex];
                        sumD += l[igi] * u[igi];
                    }

                    d[i] -= sumD;
                }
            }
            // Прямой ход Ly = F
            static void CalcDir(double[] y, double[] F)
            {
                double sum, buf;
                int n = N;

                for (int i = 0; i < n; i++)
                {
                    y[i] = F[i];
                }

                for (int i = 0; i < n; i++)
                {
                    sum = 0;

                    int begI = ig[i];
                    int endI = ig[i + 1];

                    for (int igi = begI; igi < endI; igi++)
                    {
                        sum += y[jg[igi]] * l[igi];
                    }

                    buf = y[i] - sum;
                    y[i] = buf / d[i];
                }
            }
            // Обратный ход Ux = y
            static void CalcRev(double[] x, double[] y)
            {
                int n = N;

                for (int i = 0; i < n; i++)
                {
                    x[i] = y[i];
                }

                for (int i = n - 1; i >= 0; i--)
                {
                    int begI = ig[i];
                    int endI = ig[i + 1];

                    for (int igi = begI; igi < endI; igi++)
                    {
                        x[jg[igi]] -= x[i] * u[igi];
                    }
                }
            }
            // Процедура умножения матрицы на вектор Ax = res
            static void MultMV(int[] ig, int[] jg, double[] x, double[] res)
            {
                int n = x.Length;

                for (int i = 0; i < n; i++)
                {
                    res[i] = di[i] * x[i];

                    int begI = ig[i];
                    int endI = ig[i + 1];

                    for (int igi = begI; igi < endI; igi++)
                    {
                        int Jindex = jg[igi];

                        res[i] += ggl[igi] * x[Jindex];
                        res[Jindex] += ggu[igi] * x[i];
                    }
                }
            }
            // Функция скалярного произведение двух векторов
            static double ScalarProd(double[] x, double[] y)
            {
                int n = x.Length;

                double result = 0;

                for (int i = 0; i < n; i++)
                {
                    result += x[i] * y[i];
                }

                return result;
            }
            // Локально-оптимальная схема c факторизацией LU
            public void LOS_LU()
            {
                double alpha, beta, norm, temp_nev = 0;

                int n = N, maxiter = 1000;
                double epsilon = 1e-15;

                CalcLU();
                // A * x0
                MultMV(ig, jg, q, temp);

                // f - A * x0
                for (int i = 0; i < n; i++)
                {
                    temp[i] = F[i] - temp[i];
                }

                // L * r0 = f - A * x0
                CalcDir(r, temp);

                // U * z0 = r0
                CalcRev(z, r);

                // A * z0
                MultMV(ig, jg, z, temp);

                // L * p0 = A * z0
                CalcDir(p, temp);

                norm = ScalarProd(r, r);

                int k;

                for (k = 0; k < maxiter && Math.Abs(norm) > epsilon && temp_nev != norm; k++)
                {
                    // если невязка не изменилась, то выходим из итерационного процесса
                    temp_nev = norm;

                    alpha = ScalarProd(p, r) / ScalarProd(p, p);

                    for (int i = 0; i < n; i++)
                    {
                        q[i] = q[i] + alpha * z[i];
                        r[i] = r[i] - alpha * p[i];
                    }

                    // U * temp = r
                    CalcRev(temp, r);

                    // A * U-1 * r = temp0
                    MultMV(ig, jg, temp, temp0);

                    // L * temp = A * U-1 * r 
                    CalcDir(temp, temp0);

                    beta = -1 * ScalarProd(p, temp) / ScalarProd(p, p);

                    // U * temp0 = r
                    CalcRev(temp0, r);

                    norm = norm - alpha * alpha * ScalarProd(p, p);

                    for (int i = 0; i < n; i++)
                    {
                        z[i] = temp0[i] + beta * z[i];
                        p[i] = temp[i] + beta * p[i];
                    }

                }
                Console.WriteLine("\niter: {0}\tnev: ", k - 1);
            }
            public SLAE(int Knode, List<double> di_in, int[] ig_in, int[] jg_in, List<double> al_in, List<double> au_in, List<double> b_in)
            {
                N = Knode;

                int n = N;

                di = di_in.ToArray();
                ig = ig_in;
                jg = jg_in;
                ggl = al_in.ToArray();
                ggu = au_in.ToArray();

                int size = ig[n];




                l = new double[size];
                u = new double[size];

                d = new double[n];

                F = b_in.ToArray();

                q = new double[n];
                temp = new double[n];
                temp0 = new double[n];
                r = new double[n];
                z = new double[n];
                p = new double[n];
            }
        }
        public class SLAE_Full
        {
            private double[,] A;
            private double[] b;

            public SLAE_Full()
            {
                A = new double[0, 0];
                b = Array.Empty<double>();
            }

            public double this[int iInd, int jInd]
            {
                get => A[iInd, jInd];
                set => A[iInd, jInd] = value;
            }

            public double this[int iInd]
            {
                get => b[iInd];
                set => b[iInd] = value;
            }


            public SLAE_Full(int nSLAE)
            {
                A = new double[nSLAE, nSLAE];
                b = new double[nSLAE];
            }
            public void resizeSLAE(int nSLAE)
            {
                if (nSLAE == b.Length) return;
                A = new double[nSLAE, nSLAE];
                b = new double[nSLAE];
            }

            public void solve(double[] ans)
            {
                int nSLAE = b.Length;
                if (ans.Length != nSLAE)
                    throw new Exception("Size of the input array is not compatable with size of SLAE");




                for (int i = 0; i < nSLAE; i++)
                {

                    double del = A[i, i];
                    double absDel = Math.Abs(del);
                    int iSwap = i;


                    for (int j = i + 1; j < nSLAE; j++)
                    {
                        if (absDel < Math.Abs(A[j, i]))
                        {
                            del = A[j, i];
                            absDel = Math.Abs(del);
                            iSwap = j;
                        }
                    }

                    if (iSwap != i)
                    {
                        double buf;
                        for (int j = i; j < nSLAE; j++)
                        {
                            buf = A[i, j];
                            A[i, j] = A[iSwap, j];
                            A[iSwap, j] = buf;
                        }
                        buf = b[i];
                        b[i] = b[iSwap];
                        b[iSwap] = buf;
                    }

                    for (int j = i; j < nSLAE; j++)
                        A[i, j] /= del;

                    b[i] /= del;

                    for (int j = i + 1; j < nSLAE; j++)
                    {
                        if (A[j, i] == 0) continue;

                        double el = A[j, i];
                        for (int k = i; k < nSLAE; k++)
                        {
                            A[j, k] -= A[i, k] * el;
                        }

                        b[j] -= b[i] * el;
                    }
                }

                for (int i = nSLAE - 1; i > -1; i--)
                {
                    for (int j = i + 1; j < nSLAE; j++)
                        b[i] -= ans[j] * A[i, j];
                    ans[i] = b[i];
                }
            }


            public double[] solve()
            {
                double[] ans = new double[b.Length];
                solve(ans);
                return ans;
            }
        }
        public class OutRes
        {
            public int i { get; set; }
            public double vol { get; set; }
        }
    }
}