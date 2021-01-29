using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gerene.Balanca.Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.DataSource = Balanca.ListarPortas();
            comboBox2.DataSource = Enum.GetValues(typeof(ModeloBalanca));
        }

        private Balanca _Balanca;

        private void button1_Click(object sender, EventArgs e)
        {
            //Desconecta antes
            if (_Balanca != null)
            {
                if (_Balanca.IsConectada)
                    _Balanca.Dispose();

                _Balanca = null;
            }

            _Balanca = new Balanca()
            {
                NomePorta = comboBox1.Text,
                Modelo = (ModeloBalanca)Convert.ToInt32(comboBox2.SelectedValue),
                BaudRate = (int)numericUpDown1.Value,
                Timeout = (int)numericUpDown2.Value,
                IsMonitorar = checkBox1.Checked,
                DelayMonitoramento = (int)numericUpDown3.Value
            };

            _Balanca.AoLerPeso += _Balanca_AoLerPeso;
            _Balanca.AoLancarExcessao += _Balanca_AoLancarExcessao;

            _Balanca.Conectar();
        }        

        private void button3_Click(object sender, EventArgs e)
        {
            if (_Balanca != null)
            {
                if (_Balanca.IsConectada)
                    _Balanca.Dispose();

                _Balanca = null;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_Balanca == null || !_Balanca.IsConectada)
            {
                MessageBox.Show("Balança não conectada");
                return;
            }

            _Balanca.LerPeso();

        }

        private void _Balanca_AoLerPeso(object sender, Balanca.BalancaEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                label7.Text = $"Ultimo peso {e.Peso:N3} Kg";

                textBox1.Text += $"{DateTime.Now:dd/MM/yyyy HH:mm:ss} - {e.Peso:N3} Kg" + Environment.NewLine;
            });

            Application.DoEvents();
        }

        private void _Balanca_AoLancarExcessao(object sender, Balanca.BalancaEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                textBox2.Text += $"{DateTime.Now:dd/MM/yyyy HH:mm:ss} - {e.Excessao.Message}" + Environment.NewLine;
            });

            Application.DoEvents();
        }
        

    }
}
