using System.Numerics;
using Xunit;

namespace Conde.Racional.Testes;

/// <summary>
/// Os axiomas de corpo, conferidos em <b>todas</b> as frações pequenas.
/// </summary>
/// <remarks>
/// <para>
/// Os racionais formam um corpo, e isso não é uma observação decorativa: é uma
/// lista de nove identidades que a aritmética <b>tem</b> de obedecer, e que
/// servem de juiz sem depender de implementação nenhuma.
/// </para>
/// <para>
/// Todas as frações com numerador de -12 a 12 e denominador de 1 a 12 dão <b>183</b>
/// valores distintos depois de reduzidas, e todos os pares delas são conferidos.
/// Para os triplos da associatividade o domínio é menor, 47 frações, o que dá
/// 103.823 combinações. Nada disso é amostra: são as identidades inteiras, sobre
/// um domínio escolhido pequeno de propósito.
/// </para>
/// <para>
/// E é aqui que o <c>double</c> reprova: a associatividade da soma é <b>falsa</b>
/// em ponto flutuante, e um dos testes conta em quantos casos ela quebra.
/// </para>
/// </remarks>
public class CorpoTest
{
    private static List<Fracao> TodasAsFracoes(int ate)
    {
        var todas = new HashSet<Fracao>();

        for (var cima = -ate; cima <= ate; cima++)
        {
            for (var baixo = 1; baixo <= ate; baixo++)
            {
                todas.Add(Fracao.De(cima, baixo));
            }
        }

        return todas.ToList();
    }

    [Fact(DisplayName = "as frações saem sempre na forma reduzida e canônica")]
    public void SempreReduzida()
    {
        // É a invariante que faz a classe servir como chave: 2/4 e 1/2 têm de
        // ser o MESMO valor para a igualdade e para o espalhamento.
        for (var cima = -30; cima <= 30; cima++)
        {
            for (var baixo = -30; baixo <= 30; baixo++)
            {
                if (baixo == 0)
                {
                    continue;
                }

                var fracao = Fracao.De(cima, baixo);

                // O denominador é sempre positivo.
                Assert.True(fracao.Baixo > 0, $"{cima}/{baixo} deu denominador {fracao.Baixo}");

                // Não há fator comum.
                Assert.Equal(
                    BigInteger.One,
                    BigInteger.GreatestCommonDivisor(BigInteger.Abs(fracao.Cima), fracao.Baixo));

                // O zero é sempre 0/1.
                if (fracao.Cima.IsZero)
                {
                    Assert.Equal(BigInteger.One, fracao.Baixo);
                }

                // E o valor não mudou.
                Assert.Equal(
                    (double)cima / baixo,
                    fracao.ComoDouble(),
                    10);
            }
        }

        // As formas equivalentes são o mesmo valor, com o mesmo hash.
        Assert.Equal(Fracao.De(1, 2), Fracao.De(2, 4));
        Assert.Equal(Fracao.De(1, 2), Fracao.De(-3, -6));
        Assert.Equal(Fracao.De(-1, 2), Fracao.De(1, -2));

        Assert.Equal(Fracao.De(1, 2).GetHashCode(), Fracao.De(50, 100).GetHashCode());

        var conjunto = new HashSet<Fracao> { Fracao.De(1, 2), Fracao.De(2, 4), Fracao.De(3, 6) };

        Assert.Single(conjunto);
    }

    [Fact(DisplayName = "comutatividade e elementos neutros, em TODOS os 33.489 pares")]
    public void ComutatividadeENeutros()
    {
        var fracoes = TodasAsFracoes(12);

        // 183, e nao 325. Eu contei 25 numeradores vezes 12 denominadores e
        // esqueci que a reducao COLAPSA as equivalentes: 2/4 e 1/2 viram a mesma
        // fracao, e 1/1, 2/2 e 3/3 tambem. O teste corrigiu a conta.
        Assert.Equal(183, fracoes.Count);

        foreach (var a in fracoes)
        {
            // Os neutros.
            Assert.Equal(a, a + Fracao.Zero);
            Assert.Equal(a, a * Fracao.Um);

            // O oposto.
            Assert.Equal(Fracao.Zero, a + a.Oposto());

            // O inverso, quando existe.
            if (a.Sinal != 0)
            {
                Assert.Equal(Fracao.Um, a * a.Inverso());
                Assert.Equal(a, a.Inverso().Inverso());
            }

            foreach (var b in fracoes)
            {
                // Comutatividade.
                Assert.Equal(a + b, b + a);
                Assert.Equal(a * b, b * a);

                // A subtração é a soma do oposto.
                Assert.Equal(a - b, a + b.Oposto());

                // E somar e tirar devolve o original, EXATAMENTE. É aqui que o
                // double falharia.
                Assert.Equal(a, a + b - b);

                if (b.Sinal != 0)
                {
                    Assert.Equal(a, a / b * b);
                    Assert.Equal(a / b, a * b.Inverso());
                }
            }
        }
    }

    [Fact(DisplayName = "associatividade e distributividade, em todos os triplos")]
    public void AssociatividadeEDistributividade()
    {
        // O domínio aqui é menor porque são triplos: 47 frações dão 103.823
        // combinações.
        var fracoes = TodasAsFracoes(6);

        Assert.Equal(47, fracoes.Count);

        var quantos = 0;

        foreach (var a in fracoes)
        {
            foreach (var b in fracoes)
            {
                foreach (var c in fracoes)
                {
                    // Associatividade da soma e do produto.
                    Assert.Equal((a + b) + c, a + (b + c));
                    Assert.Equal((a * b) * c, a * (b * c));

                    // Distributividade.
                    Assert.Equal(a * (b + c), a * b + a * c);

                    quantos++;
                }
            }
        }

        Assert.Equal(47 * 47 * 47, quantos);
        Assert.Equal(103_823, quantos);
    }

    [Fact(DisplayName = "a associatividade da soma é FALSA em double, e verdadeira aqui")]
    public void ODoubleQuebraAAssociatividade()
    {
        // Este é o teste que justifica a biblioteca existir. Em ponto flutuante,
        // (a + b) + c e a + (b + c) dão resultados diferentes, e isso não é um
        // caso raro: é o comportamento normal.
        var sorteio = new Random(29);

        var quebrouNoDouble = 0;

        for (var caso = 0; caso < 200_000; caso++)
        {
            var a = Fracao.De(sorteio.Next(-100, 100), sorteio.Next(1, 100));
            var b = Fracao.De(sorteio.Next(-100, 100), sorteio.Next(1, 100));
            var c = Fracao.De(sorteio.Next(-100, 100), sorteio.Next(1, 100));

            // Exato: sempre igual.
            Assert.Equal((a + b) + c, a + (b + c));

            // Em double: às vezes não.
            var da = a.ComoDouble();
            var db = b.ComoDouble();
            var dc = c.ComoDouble();

            if ((da + db) + dc != da + (db + dc))
            {
                quebrouNoDouble++;
            }
        }

        Assert.True(quebrouNoDouble > 0,
            "o double não quebrou a associatividade em 200 mil casos, o que seria "
            + "surpreendente");

        // O caso mais famoso: 0,1 + 0,2 não é 0,3.
        Assert.NotEqual(0.3, 0.1 + 0.2);

        // Aqui é.
        Assert.Equal(
            Fracao.De(3, 10),
            Fracao.De(1, 10) + Fracao.De(2, 10));

        // E um terço somado três vezes dá exatamente um.
        var terco = Fracao.De(1, 3);

        Assert.Equal(Fracao.Um, terco + terco + terco);

        // Em double tambem da, e isto o teste me ensinou. Eu tinha escrito que
        // nao dava, porque e o exemplo que todo mundo repete, e ele esta errado:
        //
        //   1.0/3 + 1.0/3 + 1.0/3  ==  1.0   ->  verdadeiro
        //
        // O double de 1/3 e um pouco MENOR que um terco, e somar tres deles cai
        // num valor que arredonda de volta para exatamente 1. Os erros se
        // cancelaram por sorte.
        Assert.Equal(1.0, 1.0 / 3 + 1.0 / 3 + 1.0 / 3);

        // E esse e o argumento de verdade contra o ponto flutuante, mais forte
        // que o exemplo errado: o erro NAO E PREVISIVEL olhando a conta. Com um
        // terco ele se cancela, com um decimo nao:
        Assert.NotEqual(0.3, 0.1 + 0.2);
        Assert.NotEqual(1.0, 0.1 + 0.1 + 0.1 + 0.1 + 0.1 + 0.1 + 0.1 + 0.1 + 0.1 + 0.1);

        // Com fracao exata, as duas dao certo e nao e preciso saber qual e qual.
        Assert.Equal(Fracao.Um, Enumerable.Repeat(Fracao.De(1, 10), 10)
            .Aggregate(Fracao.Zero, (soma, parcela) => soma + parcela));
    }

    [Fact(DisplayName = "a ordem é total e compatível com a aritmética")]
    public void AOrdem()
    {
        var fracoes = TodasAsFracoes(10);

        foreach (var a in fracoes)
        {
            foreach (var b in fracoes)
            {
                // Tricotomia: exatamente uma das três vale.
                var menor = a < b;
                var igual = a == b;
                var maior = a > b;

                Assert.True(
                    (menor ? 1 : 0) + (igual ? 1 : 0) + (maior ? 1 : 0) == 1,
                    $"{a} e {b}: menor={menor} igual={igual} maior={maior}");

                // A comparação concorda com a do double, quando o double não erra.
                var pelaConta = a.CompareTo(b);
                var peloDouble = a.ComoDouble().CompareTo(b.ComoDouble());

                if (pelaConta != 0 && peloDouble != 0)
                {
                    Assert.Equal(Math.Sign(pelaConta), Math.Sign(peloDouble));
                }

                // Somar o mesmo valor aos dois lados não muda a ordem.
                foreach (var c in fracoes.Take(20))
                {
                    Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign((a + c).CompareTo(b + c)));

                    // Multiplicar por positivo mantém, por negativo inverte.
                    if (c.Sinal > 0)
                    {
                        Assert.Equal(
                            Math.Sign(a.CompareTo(b)),
                            Math.Sign((a * c).CompareTo(b * c)));
                    }
                    else if (c.Sinal < 0)
                    {
                        Assert.Equal(
                            -Math.Sign(a.CompareTo(b)),
                            Math.Sign((a * c).CompareTo(b * c)));
                    }
                }
            }
        }
    }

    [Fact(DisplayName = "a potência, inclusive com expoente negativo")]
    public void APotencia()
    {
        var fracoes = TodasAsFracoes(8).Where(f => f.Sinal != 0).ToList();

        foreach (var a in fracoes)
        {
            Assert.Equal(Fracao.Um, a.Potencia(0));
            Assert.Equal(a, a.Potencia(1));
            Assert.Equal(a * a, a.Potencia(2));
            Assert.Equal(a * a * a, a.Potencia(3));

            // Expoente negativo é o inverso elevado ao positivo.
            Assert.Equal(a.Inverso(), a.Potencia(-1));
            Assert.Equal(a.Inverso() * a.Inverso(), a.Potencia(-2));

            // E a identidade: a^m * a^n = a^(m+n).
            for (var m = -3; m <= 3; m++)
            {
                for (var n = -3; n <= 3; n++)
                {
                    Assert.Equal(a.Potencia(m + n), a.Potencia(m) * a.Potencia(n));
                }
            }
        }

        // Zero elevado a zero é um, por convenção, e a positivo é zero.
        Assert.Equal(Fracao.Um, Fracao.Zero.Potencia(0));
        Assert.Equal(Fracao.Zero, Fracao.Zero.Potencia(3));

        // E zero elevado a negativo não existe.
        Assert.Throws<DivideByZeroException>(() => Fracao.Zero.Potencia(-1));
    }
}
