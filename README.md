# Gerene.Balanca
[![Nuget count](http://img.shields.io/nuget/v/Gerene.Balanca.svg)](https://www.nuget.org/packages/Gerene.Balanca)

Leitura de balança de checkout nativo em .Net Standard 2.0.

O que o projeto faz
-------
O projeto tem como objetivo facilitar a leitura de balanças de checkout, a ideia é fazer o mesmo processo do ACBrBal, mas nativo em .Net.



Como usar
-------
O projeto conta com um demo em .Net 5 que implementa todas suas funcionalidades.

Exemplo de leitura única:
```
var balanca = new Balanca()
{
	NomePorta = "COM1",
	Modelo = ModeloBalanca.Toledo,  //Atualmente implementa os protocolos para Toledo e Filizola
	BaudRate = 9600,
	Timeout = 500
};
balanca.Conectar(); //Conectar à porta serial

decimal peso = balanca.LerPeso(); //recuperar o peso atual

balanca.Dispose(); //Libera a porta serial
```

Exemplo para monitoramento de balança:
```
var balanca = new Balanca()
{
	NomePorta = "COM1",
	Modelo = ModeloBalanca.Toledo,
	BaudRate = 9600,
	Timeout = 500,

	IsMonitorar = true,
	DelayMonitoramento = 1000, //intervalo entre as leituras em milisegundos
};

balanca.Conectar();
balanca.AoLerPeso += Balanca_AoLerPeso;

private void Balanca_AoLerPeso(object sender, Balanca.BalancaEventArgs e)
{
   //e.Peso contem o peso lido (em caso de leitura com sucesso)
   //e.Leitura contem a string lida (já tratada)
   //e.Excecao contem a exception disparada em caso de erro
}
```

Agradecimentos
-------

Agradecimento especial aos projetos:
 - https://github.com/thiago132000/brainiac (base de estudos para comunicação serial em C#);
 - ACBrBal - Base de estudos para funcionamento de balanças de checkout;
