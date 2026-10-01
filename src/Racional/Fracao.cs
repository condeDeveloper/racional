using System.Numerics;

namespace Conde.Racional;

/// <summary>
/// Uma fração exata, sempre na forma reduzida e com o sinal no numerador.
/// </summary>
/// <remarks>
/// <para>
/// Um racional é o que um <c>double</c> não consegue ser: <c>1/3</c> aqui é
/// exatamente um terço, e somar três deles dá exatamente um. Em ponto flutuante
/// dá 0,9999999999999998.
/// </para>
/// <para>
/// A forma <b>canônica</b> é mantida em toda operação, e não só na criação: o
/// numerador e o denominador não têm fator comum, o denominador é positivo, e o
/// zero é sempre <c>0/1</c>. Sem isso, <c>2/4</c> e <c>1/2</c> seriam valores
/// diferentes para a igualdade e para a tabela de espalhamento, e a classe
/// deixaria de servir como chave.
/// </para>
/// <para>
/// O tipo por baixo é <c>BigInteger</c>, então não há estouro: somar um milhão
/// de frações com denominadores distintos faz o denominador crescer, e ele
/// cresce. O preço é que a conta fica lenta quando os números incham, e é por
/// isso que reduzir a cada passo importa tanto.
/// </para>
/// </remarks>
public readonly struct Fracao : IEquatable<Fracao>, IComparable<Fracao>
{
    /// <summary>O numerador, que carrega o sinal.</summary>
    public BigInteger Cima { get; }

    /// <summary>O denominador, sempre positivo.</summary>
    public BigInteger Baixo { get; }

    /// <summary>Zero.</summary>
    public static Fracao Zero => new(BigInteger.Zero, BigInteger.One, false);

    /// <summary>Um.</summary>
    public static Fracao Um => new(BigInteger.One, BigInteger.One, false);

    private Fracao(BigInteger cima, BigInteger baixo, bool reduzir)
    {
        if (baixo.IsZero)
        {
            throw new DivideByZeroException("uma fração não tem denominador zero");
        }

        if (reduzir)
        {
            // O sinal mora no numerador. Sem essa regra, -1/2 e 1/-2 seriam
            // valores diferentes para a igualdade.
            if (baixo.Sign < 0)
            {
                cima = -cima;
                baixo = -baixo;
            }

            if (cima.IsZero)
            {
                baixo = BigInteger.One;
            }
            else
            {
                var comum = BigInteger.GreatestCommonDivisor(BigInteger.Abs(cima), baixo);

                if (!comum.IsOne)
                {
                    cima /= comum;
                    baixo /= comum;
                }
            }
        }

        Cima = cima;
        Baixo = baixo;
    }

    /// <summary>Monta a fração, reduzindo.</summary>
    public static Fracao De(BigInteger cima, BigInteger baixo) => new(cima, baixo, true);

    /// <summary>Monta a partir de um inteiro.</summary>
    public static Fracao De(BigInteger inteiro) => new(inteiro, BigInteger.One, false);

    /// <summary>
    /// Monta a partir de um <c>decimal</c>, exatamente.
    /// </summary>
    /// <remarks>
    /// A conversão é exata porque todo <c>decimal</c> é, por construção, um
    /// racional com denominador potência de dez. O <c>double</c> não tem essa
    /// propriedade, e por isso a conversão a partir dele mora noutro método,
    /// com o nome dizendo o que acontece.
    /// </remarks>
    public static Fracao DeDecimal(decimal valor)
    {
        var partes = decimal.GetBits(valor);

        var escala = (partes[3] >> 16) & 0xFF;

        var negativo = (partes[3] & unchecked((int)0x8000_0000)) != 0;

        var cima = new BigInteger((uint)partes[0])
            | (new BigInteger((uint)partes[1]) << 32)
            | (new BigInteger((uint)partes[2]) << 64);

        if (negativo)
        {
            cima = -cima;
        }

        return De(cima, BigInteger.Pow(10, escala));
    }

    /// <summary>
    /// A fração <b>exata</b> que o <c>double</c> representa, que não é a que foi escrita.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Esta conversão não perde nada: ela devolve o valor exato que está nos 64
    /// bits. O que surpreende é que esse valor quase nunca é o que a pessoa
    /// escreveu. O <c>double</c> de <c>0.1</c> é exatamente
    /// <c>3602879701896397 / 36028797018963968</c>, e não um décimo.
    /// </para>
    /// <para>
    /// É a ferramenta certa para mostrar o problema do ponto flutuante, e a
    /// ferramenta errada para converter dados: quem tem 0,1 num <c>double</c> e
    /// quer a fração 1/10 precisa de <see cref="Aproximar"/>.
    /// </para>
    /// </remarks>
    public static Fracao DoDoubleExato(double valor)
    {
        if (double.IsNaN(valor) || double.IsInfinity(valor))
        {
            throw new ArgumentException($"{valor} não é um número racional", nameof(valor));
        }

        if (valor == 0)
        {
            return Zero;
        }

        var bits = BitConverter.DoubleToInt64Bits(valor);

        var negativo = bits < 0;

        var expoente = (int)((bits >> 52) & 0x7FF);

        var mantissa = bits & 0xF_FFFF_FFFF_FFFF;

        if (expoente == 0)
        {
            expoente++;   // subnormal
        }
        else
        {
            mantissa |= 1L << 52;   // o bit implícito
        }

        expoente -= 1075;

        var cima = new BigInteger(mantissa);

        if (negativo)
        {
            cima = -cima;
        }

        return expoente >= 0
            ? De(cima * BigInteger.Pow(2, expoente), BigInteger.One)
            : De(cima, BigInteger.Pow(2, -expoente));
    }

    /// <summary>O inverso. Zero não tem.</summary>
    public Fracao Inverso() => Cima.IsZero
        ? throw new DivideByZeroException("zero não tem inverso")
        : new Fracao(Baixo, Cima, true);

    /// <summary>O oposto.</summary>
    public Fracao Oposto() => new(-Cima, Baixo, false);

    /// <summary>O valor absoluto.</summary>
    public Fracao Absoluto() => new(BigInteger.Abs(Cima), Baixo, false);

    /// <summary>O sinal: -1, 0 ou 1.</summary>
    public int Sinal => Cima.Sign;

    /// <summary>É um inteiro?</summary>
    public bool EhInteiro => Baixo.IsOne;

    /// <summary>A parte inteira, truncada para o zero.</summary>
    public BigInteger Inteira => Cima / Baixo;

    public static Fracao operator +(Fracao um, Fracao outro) =>
        De(um.Cima * outro.Baixo + outro.Cima * um.Baixo, um.Baixo * outro.Baixo);

    public static Fracao operator -(Fracao um, Fracao outro) =>
        De(um.Cima * outro.Baixo - outro.Cima * um.Baixo, um.Baixo * outro.Baixo);

    public static Fracao operator *(Fracao um, Fracao outro) =>
        De(um.Cima * outro.Cima, um.Baixo * outro.Baixo);

    public static Fracao operator /(Fracao um, Fracao outro) => outro.Cima.IsZero
        ? throw new DivideByZeroException("não dá para dividir por zero")
        : De(um.Cima * outro.Baixo, um.Baixo * outro.Cima);

    public static Fracao operator -(Fracao valor) => valor.Oposto();

    /// <summary>Eleva a um expoente inteiro, que pode ser negativo.</summary>
    public Fracao Potencia(int expoente)
    {
        if (expoente == 0)
        {
            return Um;
        }

        if (expoente < 0)
        {
            return Inverso().Potencia(-expoente);
        }

        return De(BigInteger.Pow(Cima, expoente), BigInteger.Pow(Baixo, expoente));
    }

    public bool Equals(Fracao outra) => Cima == outra.Cima && Baixo == outra.Baixo;

    public override bool Equals(object? outro) => outro is Fracao fracao && Equals(fracao);

    public override int GetHashCode() => HashCode.Combine(Cima, Baixo);

    /// <summary>
    /// Compara sem converter para ponto flutuante.
    /// </summary>
    /// <remarks>
    /// A comparação é por multiplicação cruzada, e os dois denominadores são
    /// positivos por construção, então o sinal não vira. Converter para
    /// <c>double</c> para comparar seria mais rápido e erraria em frações muito
    /// próximas, que é exatamente o caso em que a comparação importa.
    /// </remarks>
    public int CompareTo(Fracao outra) => (Cima * outra.Baixo).CompareTo(outra.Cima * Baixo);

    public static bool operator ==(Fracao um, Fracao outro) => um.Equals(outro);

    public static bool operator !=(Fracao um, Fracao outro) => !um.Equals(outro);

    public static bool operator <(Fracao um, Fracao outro) => um.CompareTo(outro) < 0;

    public static bool operator >(Fracao um, Fracao outro) => um.CompareTo(outro) > 0;

    public static bool operator <=(Fracao um, Fracao outro) => um.CompareTo(outro) <= 0;

    public static bool operator >=(Fracao um, Fracao outro) => um.CompareTo(outro) >= 0;

    /// <summary>O valor como <c>double</c>, que é onde a exatidão acaba.</summary>
    public double ComoDouble() => (double)Cima / (double)Baixo;

    /// <summary>
    /// A melhor aproximação racional de um <c>double</c>, com denominador até o limite.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Usa frações contínuas, que dão a <b>melhor</b> aproximação possível para
    /// cada tamanho de denominador. "Melhor" aqui é um teorema e não uma
    /// heurística: nenhuma fração com denominador menor ou igual chega mais
    /// perto.
    /// </para>
    /// <para>
    /// É o que transforma 0,1 de volta em 1/10, e o que acha 355/113 para o pi,
    /// que foi descoberta por Zu Chongzhi no século V e acerta seis casas
    /// decimais.
    /// </para>
    /// </remarks>
    public static Fracao Aproximar(double valor, BigInteger limiteDoDenominador)
    {
        if (limiteDoDenominador < BigInteger.One)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limiteDoDenominador), limiteDoDenominador, "o limite é pelo menos 1");
        }

        if (double.IsNaN(valor) || double.IsInfinity(valor))
        {
            throw new ArgumentException($"{valor} não é um número racional", nameof(valor));
        }

        var negativo = valor < 0;

        valor = Math.Abs(valor);

        // Os convergentes da fração contínua, montados por recorrência.
        BigInteger cimaAnterior = 1, baixoAnterior = 0;
        BigInteger cima = (BigInteger)Math.Floor(valor), baixo = 1;

        var resto = valor - Math.Floor(valor);

        while (resto > 1e-15)
        {
            var proximo = 1.0 / resto;

            var parteInteira = (BigInteger)Math.Floor(proximo);

            var novoBaixo = parteInteira * baixo + baixoAnterior;

            if (novoBaixo > limiteDoDenominador)
            {
                break;
            }

            var novoCima = parteInteira * cima + cimaAnterior;

            (cimaAnterior, baixoAnterior) = (cima, baixo);
            (cima, baixo) = (novoCima, novoBaixo);

            resto = proximo - Math.Floor(proximo);
        }

        return De(negativo ? -cima : cima, baixo);
    }

    /// <summary>A forma escrita: <c>3/4</c>, ou só o inteiro quando for um.</summary>
    public override string ToString() => EhInteiro ? Cima.ToString() : $"{Cima}/{Baixo}";

    /// <summary>Lê a forma <c>3/4</c> ou <c>3</c>.</summary>
    public static Fracao Ler(string texto) => TentarLer(texto, out var fracao)
        ? fracao
        : throw new FormatException($"não é uma fração: \"{texto}\"");

    /// <summary>Lê a forma <c>3/4</c>, devolvendo falso em vez de lançar.</summary>
    public static bool TentarLer(string? texto, out Fracao fracao)
    {
        fracao = default;

        if (string.IsNullOrEmpty(texto))
        {
            return false;
        }

        var barra = texto.IndexOf('/');

        if (barra < 0)
        {
            if (!BigInteger.TryParse(texto, out var inteiro))
            {
                return false;
            }

            fracao = De(inteiro);

            return true;
        }

        if (!BigInteger.TryParse(texto[..barra], out var cima)
            || !BigInteger.TryParse(texto[(barra + 1)..], out var baixo)
            || baixo.IsZero)
        {
            return false;
        }

        fracao = De(cima, baixo);

        return true;
    }
}
