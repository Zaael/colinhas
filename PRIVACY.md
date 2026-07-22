# Política de Privacidade — Colinhas

**Última atualização:** 22 de julho de 2026

O Colinhas é um gerenciador de área de transferência para Windows, gratuito e de
código aberto. Esta política explica, em português claro, o que o aplicativo faz
com os seus dados.

## Resumo

O Colinhas **não coleta, não transmite e não compartilha nenhum dado seu**. Tudo o
que ele guarda fica no seu próprio computador, criptografado, e é apagado quando
você desinstala o aplicativo. Não há servidores, contas, cadastro, anúncios nem
telemetria.

## Que dados o aplicativo acessa

Para funcionar, o Colinhas monitora a área de transferência do Windows e guarda um
histórico dos **textos** que você copia. Esse histórico pode conter qualquer coisa
que você copie — inclusive informações sensíveis, como senhas ou dados pessoais,
se você copiá-las.

Além disso, o aplicativo guarda o que você mesmo cria dentro dele: os seus
templates de texto, as descrições que você dá aos itens e as suas preferências
(por exemplo, se o "colar direto" está ligado).

O Colinhas **não** acessa seus arquivos, sua câmera, seu microfone, sua localização,
seus contatos nem seu histórico de navegação.

## Onde esses dados ficam

Exclusivamente no seu computador, na pasta de dados local do aplicativo.

O histórico é gravado **criptografado** com a Data Protection API (DPAPI) do
Windows, usando uma chave derivada da sua conta de usuário. Na prática: outro
usuário do mesmo computador não consegue ler o seu histórico, e o arquivo não é
legível se for copiado para outra máquina.

Nada é enviado para a internet. O Colinhas não faz nenhuma conexão de rede.

## Compartilhamento com terceiros

Nenhum. Não existem servidores do Colinhas, nem serviços de análise, nem
publicidade, nem SDKs de terceiros coletando dados dentro do aplicativo.

## Retenção e exclusão

Você tem controle total sobre os dados:

- **Apagar um item** — remova-o do histórico dentro do aplicativo.
- **Apagar tudo** — desinstalar o Colinhas remove todos os dados junto, incluindo
  histórico, templates e preferências. Por ser um aplicativo empacotado (MSIX), o
  Windows apaga a pasta de dados na desinstalação.

## Permissões que o aplicativo pede

O Colinhas declara a permissão `runFullTrust`, necessária para os recursos de
sistema que ele usa: monitorar a área de transferência, registrar atalhos globais
de teclado e simular o Ctrl+V do "colar direto". Ela **não** é usada para acessar
seus arquivos pessoais.

## Crianças

O Colinhas é uma ferramenta de produtividade de uso geral. Ele não coleta dados de
ninguém, de qualquer idade.

## Código aberto

O código-fonte é público e pode ser auditado por qualquer pessoa:
<https://github.com/Zaael/colinhas>

Se você quiser verificar qualquer afirmação desta política, o código está lá.

## Mudanças nesta política

Se o aplicativo passar a fazer algo diferente com os seus dados, esta política será
atualizada antes da mudança chegar até você, e a data no topo será alterada. O
histórico de alterações fica visível no próprio repositório.

## Contato

Dúvidas ou preocupações sobre privacidade: **zaael.dev@gmail.com**

Você também pode abrir uma issue em
<https://github.com/Zaael/colinhas/issues>.
