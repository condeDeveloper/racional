using System.Numerics;
using Xunit;

namespace Conde.Racional.Testes;

/// <summary>
/// As conversões, e as aproximações que a história já conhecia.
/// </summary>
public class AproximarTest
{
    [Theory(DisplayName = "os convergentes do pi, que a história achou antes")]
    // Os convergentes da fração contínua do pi são conhecidos há séculos, e
    // nenhum deles é um número meu.
    [InlineData(3, "3")]
    [InlineData(7, "22/7")]              // Arquimedes, século III antes de Cristo
    // 333/106, e nao 22/7: eu supus que o convergente seguinte so apareceria
    // acima de 106, e o denominador dele e exatamente 106. Ele erra 8,3 por
    // cem mil contra 1,3 por mil do 22/7.
    [InlineData(106, "333/106")]
    [InlineData(113, "355/113")]         // Zu Chongzhi, século V
    [InlineData(33_102, "103993/33102")]
    public void OsConvergentesDoPi(int limite, string esperado)
    {
        var achado = Fracao.Aproximar(Math.PI, limite);

        Assert.Equal(esperado, achado.ToString());
    }

    [Fact(DisplayName = "355/113 acerta seis casas do pi, com três algarismos de cada lado")]
    public void AAproximacaoDeZuChongzhi()
    {
        var zu = Fracao.De(355, 113);

        var erro = Math.Abs(zu.ComoDouble() - Math.PI);

        // Menos de três décimos de milionésimo de erro.
        Assert.True(erro < 3e-7, $"o erro foi {erro}");

        // E é a MELHOR com denominador até 113: nenhuma outra chega mais perto.
        // Isso é um teorema sobre frações contínuas, e aqui está conferido na
        // marra, uma por uma.
        for (var baixo = 1; baixo <= 113; baixo++)
        {
            var cima = (BigInteger)Math.Round(Math.PI * baixo);

            var candidata = Fracao.De(cima, baixo);

            var erroDela = Math.Abs(candidata.ComoDouble() - Math.PI);

            Assert.True(erroDela >= erro - 1e-18,
                $"{candidata} chega mais perto do pi que 355/113");
        }
    }

    [Fact(DisplayName = "a aproximação devolve a fração que a pessoa escreveu")]
    public void DeVoltaAoQueFoiEscrito()
    {
        // 0,1 em double não é um décimo, mas aproximar com denominador até mil
        // devolve 1/10. É a operação certa para converter dados que vieram de um
        // double.
        Assert.Equal(Fracao.De(1, 10), Fracao.Aproximar(0.1, 1_000));
        Assert.Equal(Fracao.De(1, 3), Fracao.Aproximar(1.0 / 3, 1_000));
        Assert.Equal(Fracao.De(2, 7), Fracao.Aproximar(2.0 / 7, 1_000));
        Assert.Equal(Fracao.De(-3, 4), Fracao.Aproximar(-0.75, 1_000));
        Assert.Equal(Fracao.Zero, Fracao.Aproximar(0.0, 1_000));

        // E em escala: toda fração com denominador até 200 volta inteira.
        var quantas = 0;

        for (var baixo = 1; baixo <= 200; baixo++)
        {
            for (var cima = -20; cima <= 20; cima++)
            {
                var original = Fracao.De(cima, baixo);

                var devolta = Fracao.Aproximar(original.ComoDouble(), 1_000);

                Assert.True(original == devolta,
                    $"{original} virou {devolta} na ida e volta pelo double");

                quantas++;
            }
        }

        Assert.Equal(200 * 41, quantas);
    }

    [Fact(DisplayName = "o double exato mostra o que está mesmo nos 64 bits")]
    public void ODoubleExato()
    {
        // Esta é a conversão que não perde nada e surpreende: ela devolve o valor
        // que ESTÁ no double, que quase nunca é o que a pessoa escreveu.
        var umDecimo = Fracao.DoDoubleExato(0.1);

        Assert.Equal(
            Fracao.De(
                BigInteger.Parse("3602879701896397"),
                BigInteger.Parse("36028797018963968")),
            umDecimo);

        // Ele é maior que um décimo, por pouquíssimo.
        Assert.True(umDecimo > Fracao.De(1, 10));

        var diferenca = umDecimo - Fracao.De(1, 10);

        Assert.True(diferenca.ComoDouble() < 1e-17);
        Assert.True(diferenca.Sinal > 0);

        // E o denominador é potência de dois, sempre: é o que um double é.
        for (var i = 1; i <= 100; i++)
        {
            var exato = Fracao.DoDoubleExato(1.0 / i);

            var baixo = exato.Baixo;

            while (baixo.IsEven)
            {
                baixo /= 2;
            }

            Assert.True(baixo.IsOne,
                $"o denominador de 1/{i} em double não é potência de dois: {exato.Baixo}");
        }

        // Os que o double representa exatamente voltam redondos.
        Assert.Equal(Fracao.De(1, 2), Fracao.DoDoubleExato(0.5));
        Assert.Equal(Fracao.De(1, 4), Fracao.DoDoubleExato(0.25));
        Assert.Equal(Fracao.De(3), Fracao.DoDoubleExato(3.0));
        Assert.Equal(Fracao.Zero, Fracao.DoDoubleExato(0.0));
        Assert.Equal(Fracao.De(-7, 8), Fracao.DoDoubleExato(-0.875));

        Assert.Throws<ArgumentException>(() => Fracao.DoDoubleExato(double.NaN));
        Assert.Throws<ArgumentException>(() => Fracao.DoDoubleExato(double.PositiveInfinity));
    }

    [Fact(DisplayName = "a conversão de decimal é exata, porque decimal é racional")]
    public void ODecimalEhExato()
    {
        // Todo decimal é, por construção, um racional com denominador potência
        // de dez. A conversão não aproxima nada.
        Assert.Equal(Fracao.De(1, 10), Fracao.DeDecimal(0.1m));
        Assert.Equal(Fracao.De(3, 10), Fracao.DeDecimal(0.3m));
        Assert.Equal(Fracao.De(1, 2), Fracao.DeDecimal(0.5m));
        Assert.Equal(Fracao.De(-123, 100), Fracao.DeDecimal(-1.23m));
        Assert.Equal(Fracao.Zero, Fracao.DeDecimal(0m));
        Assert.Equal(Fracao.De(1_000_000), Fracao.DeDecimal(1_000_000m));

        var sorteio = new Random(29);

        for (var caso = 0; caso < 50_000; caso++)
        {
            var valor = sorteio.Next(-1_000_000, 1_000_000) / 10_000m;

            var fracao = Fracao.DeDecimal(valor);

            // A volta é exata.
            Assert.Equal(valor, (decimal)fracao.Cima / (decimal)fracao.Baixo);
        }

        // E a diferença para o double: 0,1 em decimal é um décimo, em double não.
        Assert.Equal(Fracao.De(1, 10), Fracao.DeDecimal(0.1m));
        Assert.NotEqual(Fracao.De(1, 10), Fracao.DoDoubleExato(0.1));
    }

    [Fact(DisplayName = "ler e escrever")]
    public void LerEEscrever()
    {
        Assert.Equal("3/4", Fracao.De(3, 4).ToString());
        Assert.Equal("-3/4", Fracao.De(-3, 4).ToString());
        Assert.Equal("-3/4", Fracao.De(3, -4).ToString());
        Assert.Equal("5", Fracao.De(5).ToString());
        Assert.Equal("0", Fracao.Zero.ToString());
        Assert.Equal("1/2", Fracao.De(50, 100).ToString());

        Assert.Equal(Fracao.De(3, 4), Fracao.Ler("3/4"));
        Assert.Equal(Fracao.De(3, 4), Fracao.Ler("6/8"));
        Assert.Equal(Fracao.De(5), Fracao.Ler("5"));
        Assert.Equal(Fracao.De(-1, 2), Fracao.Ler("-1/2"));
        Assert.Equal(Fracao.De(-1, 2), Fracao.Ler("1/-2"));

        // A ida e volta pelo texto, em escala.
        var sorteio = new Random(7);

        for (var caso = 0; caso < 50_000; caso++)
        {
            var fracao = Fracao.De(sorteio.Next(-10_000, 10_000), sorteio.Next(1, 10_000));

            Assert.Equal(fracao, Fracao.Ler(fracao.ToString()));
        }

        foreach (var ruim in (string?[])["", "abc", "1/", "/2", "1/0", "1/2/3", null])
        {
            Assert.False(Fracao.TentarLer(ruim, out _), $"aceitou \"{ruim}\"");
        }

        Assert.Throws<FormatException>(() => Fracao.Ler("1/0"));
    }

    [Fact(DisplayName = "as bordas")]
    public void AsBordas()
    {
        Assert.Throws<DivideByZeroException>(() => Fracao.De(1, 0));
        Assert.Throws<DivideByZeroException>(() => Fracao.Zero.Inverso());
        Assert.Throws<DivideByZeroException>(() => Fracao.Um / Fracao.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() => Fracao.Aproximar(1.5, 0));
        Assert.Throws<ArgumentException>(() => Fracao.Aproximar(double.NaN, 100));

        // Números enormes não estouram, porque o tipo por baixo é BigInteger.
        var grande = Fracao.De(
            BigInteger.Pow(10, 100),
            BigInteger.Pow(3, 100));

        Assert.Equal(grande, grande + Fracao.Zero);
        Assert.Equal(Fracao.Um, grande / grande);
        Assert.Equal(Fracao.Zero, grande - grande);

        // Somar mil frações com denominadores distintos faz o denominador
        // crescer, e ele cresce sem estourar.
        var soma = Fracao.Zero;

        for (var i = 1; i <= 300; i++)
        {
            soma += Fracao.De(1, i);
        }

        // A série harmônica até 300 está entre 6,2 e 6,3.
        Assert.InRange(soma.ComoDouble(), 6.2, 6.3);

        // E ela é exata: o denominador é o mínimo múltiplo comum de 1 a 300.
        Assert.True(soma.Baixo > BigInteger.Pow(10, 100),
            "o denominador da harmônica até 300 devia ser enorme");

        // A parte inteira e o sinal.
        Assert.Equal(new BigInteger(3), Fracao.De(7, 2).Inteira);
        Assert.Equal(new BigInteger(-3), Fracao.De(-7, 2).Inteira);
        Assert.Equal(1, Fracao.De(1, 2).Sinal);
        Assert.Equal(-1, Fracao.De(-1, 2).Sinal);
        Assert.Equal(0, Fracao.Zero.Sinal);
        Assert.True(Fracao.De(4, 2).EhInteiro);
        Assert.False(Fracao.De(1, 2).EhInteiro);
        Assert.Equal(Fracao.De(1, 2), Fracao.De(-1, 2).Absoluto());
    }
}
