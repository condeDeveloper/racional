using System.Diagnostics;
using System.Numerics;
using Conde.Racional;

namespace Conde.Racional.Ferramentas.Medidor;

/// <summary>
/// A ferramenta: mostra o erro do ponto flutuante e mede o preco da exatidao.
/// </summary>
/// <remarks>
/// Tudo que depende de relogio mora aqui, e nao na suite de testes. Uma
/// afirmacao de tempo falha sem que nada tenha piorado, basta a maquina do CI
/// estar ocupada. Esta ferramenta imprime e sai com zero, sempre.
/// </remarks>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var comandos = new[] { "double", "pi", "harmonica", "aproximar", "velocidade" };

        var onde = Array.FindIndex(argumentos, a => comandos.Contains(a));

        if (onde < 0)
        {
            Console.WriteLine(Ajuda);

            return 0;
        }

        switch (argumentos[onde])
        {
            case "double":
                ODouble();
                break;

            case "pi":
                OPi();
                break;

            case "harmonica":
                Harmonica();
                break;

            case "aproximar":
                Aproximar();
                break;

            default:
                Velocidade();
                break;
        }

        return 0;
    }

    /// <summary>O que esta mesmo dentro de um double.</summary>
    private static void ODouble()
    {
        Console.WriteLine("O que esta mesmo dentro de um double");
        Console.WriteLine();
        Console.WriteLine("  escrito        o valor EXATO que esta nos 64 bits");
        Console.WriteLine("  " + new string('-', 76));

        foreach (var valor in new[] { 0.1, 0.2, 0.3, 0.5, 1.0 / 3, 0.7 })
        {
            var exato = Fracao.DoDoubleExato(valor);

            Console.WriteLine($"  {valor,-14:R} {exato}");
        }

        Console.WriteLine();
        Console.WriteLine("  Repare que todo denominador e potencia de dois: e isso que um double");
        Console.WriteLine("  e, e por isso que um decimo nao cabe nele.");
        Console.WriteLine();

        var umDecimo = Fracao.DoDoubleExato(0.1);

        var diferenca = umDecimo - Fracao.De(1, 10);

        Console.WriteLine($"  O double de 0,1 e MAIOR que um decimo por {diferenca}");
        Console.WriteLine($"  ou seja, por cerca de {diferenca.ComoDouble():E3}");
        Console.WriteLine();

        Console.WriteLine("  E o que acontece ao somar:");
        Console.WriteLine();
        Console.WriteLine("    conta                         em double              exato");
        Console.WriteLine("    " + new string('-', 66));

        Console.WriteLine($"    0.1 + 0.2 == 0.3              {0.1 + 0.2 == 0.3,-18} "
            + $"{Fracao.De(1, 10) + Fracao.De(2, 10) == Fracao.De(3, 10)}");

        var dezDecimos = 0.0;

        for (var i = 0; i < 10; i++)
        {
            dezDecimos += 0.1;
        }

        var dezExatos = Fracao.Zero;

        for (var i = 0; i < 10; i++)
        {
            dezExatos += Fracao.De(1, 10);
        }

        Console.WriteLine($"    0.1 somado dez vezes == 1     {dezDecimos == 1.0,-18} "
            + $"{dezExatos == Fracao.Um}");

        Console.WriteLine($"    1/3 somado tres vezes == 1    {1.0 / 3 + 1.0 / 3 + 1.0 / 3 == 1.0,-18} "
            + $"{Fracao.De(1, 3) + Fracao.De(1, 3) + Fracao.De(1, 3) == Fracao.Um}");

        Console.WriteLine();
        Console.WriteLine("  A terceira linha e a mais instrutiva, e eu tinha escrito um teste");
        Console.WriteLine("  dizendo que ela dava falso. Da VERDADEIRO: o double de 1/3 e um pouco");
        Console.WriteLine("  menor que um terco, e somar tres deles arredonda de volta para 1.");
        Console.WriteLine();
        Console.WriteLine("  Isso e o argumento de verdade contra o ponto flutuante, mais forte que");
        Console.WriteLine("  o 0.1 + 0.2: o erro NAO E PREVISIVEL olhando a conta. Com um terco ele");
        Console.WriteLine("  se cancela, com um decimo nao, e nao da para saber sem testar.");
    }

    /// <summary>Os convergentes do pi.</summary>
    private static void OPi()
    {
        Console.WriteLine("As melhores aproximacoes racionais do pi");
        Console.WriteLine();
        Console.WriteLine("  limite do        melhor        valor            erro      quem achou");
        Console.WriteLine("  denominador      fracao");
        Console.WriteLine("  " + new string('-', 76));

        (int Limite, string Quem)[] marcos =
        [
            (1, "a parte inteira"),
            (7, "Arquimedes, seculo III a.C."),
            (57, ""),
            (106, ""),
            (113, "Zu Chongzhi, seculo V"),
            (33_102, ""),
            (100_000, ""),
        ];

        Fracao? anterior = null;

        foreach (var (limite, quem) in marcos)
        {
            var fracao = Fracao.Aproximar(Math.PI, limite);

            if (anterior is { } antes && antes == fracao)
            {
                continue;
            }

            anterior = fracao;

            var erro = Math.Abs(fracao.ComoDouble() - Math.PI);

            Console.WriteLine(
                $"  {limite,11:N0}   {fracao,-13} {fracao.ComoDouble(),-14:F10} {erro,10:E2}   {quem}");
        }

        Console.WriteLine();
        Console.WriteLine("  Cada uma e a MELHOR possivel com aquele denominador: nenhuma outra");
        Console.WriteLine("  fracao de denominador menor ou igual chega mais perto. Isso e um");
        Console.WriteLine("  teorema sobre fracoes continuas, e um dos testes o confere na marra");
        Console.WriteLine("  para o 355/113, candidata por candidata.");
        Console.WriteLine();
        Console.WriteLine("  O 355/113 e notavel: tres algarismos de cada lado, e acerta seis casas");
        Console.WriteLine("  decimais. Zu Chongzhi o achou no seculo V, e a Europa so chegou la mil");
        Console.WriteLine("  anos depois.");
    }

    /// <summary>A serie harmonica, exata.</summary>
    private static void Harmonica()
    {
        Console.WriteLine("A serie harmonica, somada exatamente");
        Console.WriteLine();
        Console.WriteLine("  ate      valor           algarismos do    erro do double");
        Console.WriteLine("           aproximado      denominador");
        Console.WriteLine("  " + new string('-', 66));

        var exata = Fracao.Zero;
        var emDouble = 0.0;

        var proximo = 10;

        for (var i = 1; i <= 1_000; i++)
        {
            exata += Fracao.De(1, i);
            emDouble += 1.0 / i;

            if (i == proximo)
            {
                var algarismos = exata.Baixo.ToString().Length;

                var erro = Math.Abs(emDouble - exata.ComoDouble());

                Console.WriteLine(
                    $"  {i,5}   {exata.ComoDouble(),-14:F10}  {algarismos,10}      {erro,12:E3}");

                proximo *= 10;
            }
        }

        Console.WriteLine();
        Console.WriteLine("  O denominador cresce MUITO: somar os mil primeiros termos da um");
        Console.WriteLine("  denominador de mais de quatrocentos algarismos, porque ele e o minimo");
        Console.WriteLine("  multiplo comum de 1 a 1000.");
        Console.WriteLine();
        Console.WriteLine("  E esse e o preco da exatidao. Uma fracao exata nunca perde precisao,");
        Console.WriteLine("  e em troca ela pode ficar lenta: a conta nao estoura, ela incha.");
        Console.WriteLine();
        Console.WriteLine("  Quem soma muitas fracoes de denominadores diferentes precisa saber");
        Console.WriteLine("  disso. Para dinheiro, que tem denominador fixo, o problema nao existe.");
    }

    /// <summary>Aproximar numeros conhecidos.</summary>
    private static void Aproximar()
    {
        Console.WriteLine("A melhor fracao para cada numero, com denominador ate mil");
        Console.WriteLine();
        Console.WriteLine("  numero                valor           fracao       erro");
        Console.WriteLine("  " + new string('-', 68));

        (string Nome, double Valor)[] numeros =
        [
            ("pi", Math.PI),
            ("e", Math.E),
            ("raiz de 2", Math.Sqrt(2)),
            ("razao aurea", (1 + Math.Sqrt(5)) / 2),
            ("0,1", 0.1),
            ("1/3", 1.0 / 3),
            ("um semitom", Math.Pow(2, 1.0 / 12)),
        ];

        foreach (var (nome, valor) in numeros)
        {
            var fracao = Fracao.Aproximar(valor, 1_000);

            Console.WriteLine(
                $"  {nome,-20} {valor,-15:F10} {fracao,-12} {Math.Abs(fracao.ComoDouble() - valor),10:E2}");
        }

        Console.WriteLine();
        Console.WriteLine("  Repare na razao aurea: 1597/987. Os dois sao numeros de FIBONACCI, e");
        Console.WriteLine("  isso nao e coincidencia. A fracao continua dela e so de uns, entao os");
        Console.WriteLine("  convergentes sao exatamente as razoes entre termos vizinhos da");
        Console.WriteLine("  sequencia, e ela e o numero mais dificil de aproximar que existe: com");
        Console.WriteLine("  todos os termos iguais a 1, os denominadores crescem o mais devagar");
        Console.WriteLine("  possivel.");
        Console.WriteLine();
        Console.WriteLine("  (O erro dela na tabela nao e o maior, porque o 987 e um denominador");
        Console.WriteLine("  grande perto do limite de mil. A dificuldade aparece na comparacao");
        Console.WriteLine("  justa: com denominador ate 10, o pi erra 1,3 por mil com 22/7, e a");
        Console.WriteLine("  razao aurea erra 7,0 por mil com 13/8, que tambem sao Fibonacci.)");
        Console.WriteLine();
        Console.WriteLine("  E o semitom e a razao que a musica usa: 2^(1/12) e irracional, entao");
        Console.WriteLine("  o temperamento igual do piano NUNCA afina perfeitamente. A quinta");
        Console.WriteLine("  justa seria 3/2, e o piano toca algo um pouquinho diferente.");
    }

    /// <summary>O preco da exatidao, medido.</summary>
    private static void Velocidade()
    {
        Console.WriteLine("O preco da exatidao");
        Console.WriteLine();

        const int quantas = 200_000;

        var sorteio = new Random(29);

        var fracoes = Enumerable.Range(0, quantas)
            .Select(_ => Fracao.De(sorteio.Next(-1000, 1000), sorteio.Next(1, 1000)))
            .ToArray();

        var doubles = fracoes.Select(f => f.ComoDouble()).ToArray();

        var relogio = Stopwatch.StartNew();

        var somaExata = Fracao.Zero;

        for (var i = 0; i < quantas - 1; i++)
        {
            somaExata = fracoes[i] + fracoes[i + 1];
        }

        var tempoExato = relogio.Elapsed.TotalMilliseconds;

        relogio.Restart();

        var somaDouble = 0.0;

        for (var i = 0; i < quantas - 1; i++)
        {
            somaDouble = doubles[i] + doubles[i + 1];
        }

        var tempoDouble = relogio.Elapsed.TotalMilliseconds;

        Console.WriteLine("  operacao                    exato        double      razao");
        Console.WriteLine("  " + new string('-', 58));
        Console.WriteLine($"  somar duas fracoes     {tempoExato,8:F1} ms {tempoDouble,9:F2} ms "
            + $"{tempoExato / Math.Max(tempoDouble, 0.001),8:F0}x");

        relogio.Restart();

        for (var i = 0; i < quantas - 1; i++)
        {
            somaExata = fracoes[i] * fracoes[i + 1];
        }

        var multExato = relogio.Elapsed.TotalMilliseconds;

        relogio.Restart();

        for (var i = 0; i < quantas - 1; i++)
        {
            somaDouble = doubles[i] * doubles[i + 1];
        }

        var multDouble = relogio.Elapsed.TotalMilliseconds;

        Console.WriteLine($"  multiplicar duas       {multExato,8:F1} ms {multDouble,9:F2} ms "
            + $"{multExato / Math.Max(multDouble, 0.001),8:F0}x");

        Console.WriteLine();
        Console.WriteLine($"  (controle: {somaExata} e {somaDouble:F6})");
        Console.WriteLine();
        Console.WriteLine("  A exatidao custa duas ordens de grandeza, e e um preco justo quando");
        Console.WriteLine("  ela importa. O que a tabela nao mostra e o custo que CRESCE: somar");
        Console.WriteLine("  fracoes de denominadores distintos faz o denominador inchar, e a");
        Console.WriteLine("  conta vai ficando mais lenta a cada passo.");
        Console.WriteLine();
        Console.WriteLine("  Reduzir a cada operacao, que e o que esta classe faz, e o que segura");
        Console.WriteLine("  esse crescimento. Sem isso, mil somas deixariam a fracao impraticavel.");
    }

    private const string Ajuda = """
        racional -- aritmetica exata com fracoes

          medidor double       o que esta mesmo dentro de um double
          medidor pi           as melhores aproximacoes racionais do pi
          medidor harmonica    a serie harmonica somada exatamente
          medidor aproximar    a melhor fracao para numeros conhecidos
          medidor velocidade   o preco da exatidao
        """;
}
