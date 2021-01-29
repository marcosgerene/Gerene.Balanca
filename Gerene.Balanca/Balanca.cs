using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Gerene.Balanca
{
    public enum ModeloBalanca
    {
        Toledo,
        Filizola
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
        #endregion

        #region Construtor
        public Balanca()
        {
            _Serial = new SerialPort();
            Modelo = ModeloBalanca.Toledo;
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
            public string Leitura { get; set; }
            public decimal? Peso { get; private set; }
            public Exception Excecao { get; set; }

            public BalancaEventArgs(string leitura, decimal peso)
            {
                Leitura = leitura;
                Peso = peso;
            }

            public BalancaEventArgs(string leitura, Exception exception)
            {
                Leitura = leitura;
                Excecao = exception;
            }
        }

        public event EventHandler<BalancaEventArgs> AoLerPeso;
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

            string leitura = null;

            try
            {
                if (_Serial == null || !_Serial.IsOpen)
                    throw new ArgumentException("A porta serial não está aberta");

                leitura = LerPortaSerial();

                if (string.IsNullOrEmpty(leitura))
                    return 0;

                pesolido = TratarLeitura(leitura);

                if (AoLerPeso != null)
                    AoLerPeso.Invoke(this, new BalancaEventArgs(leitura, pesolido));

                return pesolido;
            }
            catch (Exception ex)
            {
                if (AoLerPeso != null)
                    AoLerPeso.Invoke(this, new BalancaEventArgs(leitura, ex));

                throw;
            }
        }

        private string LerPortaSerial()
        {
            //faz a leitura da porta serial
            string leitura = _Serial.ReadExisting();

            //A balança pode trabalhar de forma ativa e passiva, em caso de forma passiva a aplicação deve solicitar o peso
            if (string.IsNullOrEmpty(leitura))
            {
                switch (Modelo)
                {
                    case ModeloBalanca.Toledo:
                    case ModeloBalanca.Filizola:
                        _Serial.Write(new byte[] { 0x05 }, 0, 1);
                        return _Serial.ReadExisting();

                    default:
                        return leitura;
                }
            }

            return leitura;
        }

        private decimal TratarLeitura(string leitura)
        {
            switch (Modelo)
            {
                case ModeloBalanca.Toledo:
                    leitura = leitura.Substring(leitura.Length - 6, 5);

                    switch (leitura)
                    {
                        case "IIIII": throw new ArgumentException("Peso instável");
                        case "NNNNN": throw new ArgumentException("Peso negativo");
                        case "SSSSS": throw new ArgumentException("Sobrecarga");
                    }

                    return decimal.Parse(leitura) / 1000M;

                case ModeloBalanca.Filizola:
                    leitura = leitura.Substring(leitura.Length - 5);

                    switch (leitura[0])
                    {
                        case 'I': throw new ArgumentException("Peso instável");
                        case 'N': throw new ArgumentException("Peso negativo");
                        case 'S': throw new ArgumentException("Sobrecarga");
                    }

                    return decimal.Parse(leitura) / 1000M;

                default:
                    throw new ArgumentException($"Modelo \"{Modelo}\" não implementado");
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
                        //Não é necessário o tratamento no monitoramento, o LerPeso() faz o tratamento e lança a excessão tratada ao usuário
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
