# racional

Aritmética exata com frações em C# e .NET 8, com os axiomas de corpo conferidos
exaustivamente.

```
$ dotnet medidor.dll double

  escrito        o valor EXATO que esta nos 64 bits
  ----------------------------------------------------------------------------
  0.1            3602879701896397/36028797018963968
  0.2            3602879701896397/18014398509481984
  0.3            5404319552844595/18014398509481984
  0.5            1/2
  0.3333333...   6004799503160661/18014398509481968
```

Todo denominador é potência de dois. É isso que um `double` é, e é por isso que
um décimo não cabe nele.

## O juiz são os axiomas de corpo

Os racionais formam um corpo, e isso é uma lista de identidades que a aritmética
tem de obedecer, sem depender de implementação nenhuma: comutatividade,
associatividade, distributividade, elementos neutros, oposto e inverso.

Todas as frações com numerador de -12 a 12 e denominador de 1 a 12 dão **183**
valores distintos depois de reduzidas, e todos os pares delas são conferidos. Para
os triplos da associatividade são 47 frações, o que dá **103.823** combinações.
Nada disso é amostra: são as identidades inteiras, sobre um domínio escolhido
pequeno de propósito.

Eu tinha contado 325 e 85 frações, esquecendo que a redução colapsa as
equivalentes. O teste corrigiu a conta.

São 17 testes.

## O exemplo que todo mundo repete está errado

Escrevi um teste afirmando que `1.0/3 + 1.0/3 + 1.0/3` não dá 1 em `double`. Ele
falhou: **dá**.

```
    conta                         em double              exato
    ------------------------------------------------------------------
    0.1 + 0.2 == 0.3              False                  True
    0.1 somado dez vezes == 1     False                  True
    1/3 somado tres vezes == 1    True                   True
```

O `double` de 1/3 é um pouco menor que um terço, e somar três deles cai num valor
que arredonda de volta para exatamente 1. Os erros se cancelaram por sorte.

Isso é um argumento melhor contra o ponto flutuante que o `0.1 + 0.2`: o erro
**não é previsível olhando a conta**. Com um terço ele se cancela, com um décimo
não, e não dá para saber sem testar.

## As aproximações que a história já conhecia

```
$ dotnet medidor.dll pi

  limite do        melhor        valor            erro      quem achou
  denominador      fracao
  ----------------------------------------------------------------------------
            1   3             3.0000000000    1.42E-001   a parte inteira
            7   22/7          3.1428571429    1.26E-003   Arquimedes, seculo III a.C.
          106   333/106       3.1415094340    8.32E-005
          113   355/113       3.1415929204    2.67E-007   Zu Chongzhi, seculo V
       33,102   103993/33102  3.1415926530    5.78E-010
```

Cada uma é a **melhor possível** com aquele denominador: nenhuma outra fração de
denominador menor ou igual chega mais perto. Isso é um teorema sobre frações
contínuas, e um dos testes o confere na marra para o 355/113, candidata por
candidata.

O 355/113 é notável: três algarismos de cada lado, e acerta seis casas decimais.
Zu Chongzhi o achou no século V, e a Europa só chegou lá mil anos depois.

Eu tinha escrito que com limite 106 a melhor era 22/7, supondo que o convergente
seguinte só apareceria acima disso. O denominador dele é exatamente 106, e ele
erra quinze vezes menos.

## A razão áurea e Fibonacci

```
$ dotnet medidor.dll aproximar

  numero                valor           fracao       erro
  --------------------------------------------------------------------
  pi                   3.1415926536    355/113       2.67E-007
  razao aurea          1.6180339887    1597/987      4.59E-007
  um semitom           1.0594630944    196/185       3.63E-006
```

1597 e 987 são números de Fibonacci, e não é coincidência: a fração contínua da
razão áurea é só de uns, então os convergentes são exatamente as razões entre
termos vizinhos da sequência. Isso faz dela o número **mais difícil de aproximar**
que existe, porque com todos os termos iguais a 1 os denominadores crescem o mais
devagar possível.

E o semitom é a razão que a música usa. Como `2^(1/12)` é irracional, o
temperamento igual do piano **nunca** afina perfeitamente: a quinta justa seria
3/2, e o piano toca algo um pouquinho diferente.

## O preço da exatidão

```
$ dotnet medidor.dll harmonica

  ate      valor           algarismos do    erro do double
           aproximado      denominador
  ------------------------------------------------------------------
     10   2.9289682540          4
    100   5.1873775176         41
   1000   7.4854708606        433
```

Somar os mil primeiros termos da série harmônica dá um denominador de mais de
quatrocentos algarismos, porque ele é o mínimo múltiplo comum de 1 a 1000.

Esse é o preço. Uma fração exata nunca perde precisão, e em troca ela pode ficar
lenta: a conta não estoura, ela **incha**. Reduzir a cada operação, que é o que a
classe faz, é o que segura esse crescimento.

Para dinheiro, que tem denominador fixo, o problema não existe.

## A forma canônica não é opcional

O numerador e o denominador nunca têm fator comum, o denominador é sempre
positivo, e o zero é sempre `0/1`. Sem isso, `2/4` e `1/2` seriam valores
diferentes para a igualdade e para a tabela de espalhamento, e a classe deixaria
de servir como chave.

A manutenção é em **toda operação**, e não só na criação.

## Rodar

.NET 8. Zero dependências fora do xUnit, e só nos testes.

```
dotnet test testes/Racional.Testes/Racional.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- double
```

As medidas de tempo moram na ferramenta e não na suíte, de propósito: uma
afirmação de relógio falha sem que nada tenha piorado, basta a máquina do CI estar
ocupada.

## O que ele não faz

Não tem raiz, logaritmo nem funções trigonométricas, porque o resultado delas
quase nunca é racional. A raiz de 2 não é uma fração, e foi essa descoberta que
abalou a escola pitagórica.

Não tem números algébricos nem aritmética de intervalos, que são as duas saídas
sérias para quem precisa de exatidão além dos racionais.

Não é rápido. A exatidão custa duas ordens de grandeza contra o `double`, e o
custo cresce junto com o denominador.

Não faz conversão implícita de `double`, de propósito: há dois métodos com nomes
diferentes, porque converter um `double` é uma decisão e não uma formalidade.

## Licença

MIT.
