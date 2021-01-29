using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Gerene.Balanca
{
    /// <summary>
    /// Modelo 1 - Protocolo Toledo e similares
    /// Modelo 2 - Protocolo Filizola e similares
    /// </summary>
    public enum ModeloBalanca
    {
        Modelo1,
        Modelo2
    }

    public class Balanca : IDisposable
    {
        #region Atributos
        private SerialPort _Serial { get; set; }
        public ModeloBalanca Modelo { get; set; }
        public string NomePorta { get; set; }
        public int BaudRate { get; set; }
        public int Timeout { get; set; }
        private bool _IsMonitorar;
        public bool IsMonitorar
        {
            get => _IsMonitorar;
            set
            {
                _IsMonitorar = value;
            }
        }
        public int DelayMonitoramento { get; set; }

        public DateTime UltimaLeitura { get; private set; }
        public decimal UltimoPeso { get; private set; }
        #endregion

        #region Construtor
        public Balanca()
        {
            _Serial = new SerialPort();
            Modelo = ModeloBalanca.Modelo1;
            NomePorta = "COM1";
            BaudRate = 9600;
            Timeout = 300;

            _IsMonitorar = false;
            DelayMonitoramento = 1000;
        }

        ~Balanca() => Dispose();

        public void Dispose()
        {
            //Para o monitoramento
            if (_Cancelamento != null)
                _Cancelamento.Cancel();

            //Libera a porta serial
            if (_Serial != null)
            {
                _Serial.Dispose();
                _Serial = null;
            }
        }
        #endregion

        #region Eventos
        public class BalancaEventArgs : EventArgs
        {
            public decimal Peso { get; private set; }
            public Exception Excessao { get; set; }

            public BalancaEventArgs(decimal peso)
            {
                Peso = peso;
            }

            public BalancaEventArgs(Exception exception)
            {
                Excessao = exception;
            }
        }

        public event EventHandler<BalancaEventArgs> AoLerPeso;
        public event EventHandler<BalancaEventArgs> AoLancarExcessao;
        #endregion

        #region Métodos
        public static string[] ListarPortas() => SerialPort.GetPortNames();

        public void Conectar()
        {
            if (_Serial.IsOpen)
                throw new ArgumentException("A porta já está aberta");

            _Serial.PortName = NomePorta;
            _Serial.BaudRate = BaudRate;
            _Serial.ReadTimeout = Timeout;
            _Serial.Open();

            if (_IsMonitorar)
            {
                _Cancelamento = new CancellationTokenSource();
                Monitorar();
            }
        }

        public decimal LerPeso()
        {
            decimal pesolido = 0;

            try
            {
                if (_Serial == null || !_Serial.IsOpen)
                    throw new ArgumentException("A porta serial não está aberta");

                string dados = _Serial.ReadExisting();

                if (string.IsNullOrEmpty(dados))
                    return 0;

                switch (Modelo)
                {
                    case ModeloBalanca.Modelo1:
                        dados = dados.Substring(dados.Length - 6, 5);
                        break;
                    case ModeloBalanca.Modelo2:
                        dados = dados.Substring(dados.Length - 5);                        
                        break;
                }

                pesolido = decimal.Parse(dados) / 1000M;

                UltimoPeso = pesolido;
                UltimaLeitura = DateTime.Now;

                if (AoLerPeso != null)
                    AoLerPeso.Invoke(this, new BalancaEventArgs(pesolido));

                return pesolido;
            }
            catch (Exception ex)
            {
                if (AoLancarExcessao != null)
                    AoLancarExcessao.Invoke(this, new BalancaEventArgs(ex));

                throw;
            }
        }

        private CancellationTokenSource _Cancelamento;

        private async void Monitorar()
        {
            await Task.Run(async () =>
            {
                while (_IsMonitorar && !_Cancelamento.Token.IsCancellationRequested)
                {
                    try
                    {
                        LerPeso();
                    }
                    catch
                    {
                        //Não preciso fazer tratamentos aqui, o próprio ler peso chama o evento "AoLancarExcessao" para o usuário tratar
                    }
                    finally
                    {
                        await Task.Delay(DelayMonitoramento);
                    }
                }
            }, _Cancelamento.Token);
        }

        public bool IsConectada => _Serial != null && _Serial.IsOpen;
        #endregion

    }
}
