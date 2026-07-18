# ☕ Colinhas

> Um gerenciador de área de transferência para Windows que vai além do `Win + V`.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Plataforma-Windows%2010%2F11-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203-512BD4)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-apoie-FFDD00?logo=buymeacoffee&logoColor=black)](https://buymeacoffee.com/zaael)

O **Colinhas** é um app leve que roda em segundo plano e melhora a forma como você lida com o que copia e cola no dia a dia: histórico pesquisável, itens fixados, conteúdos ocultáveis e **templates de texto com campos variáveis**.

---

## ✨ Funcionalidades

- 📋 **Histórico da área de transferência** — captura tudo que você copia (do sistema inteiro, não só quando o app está em foco) e integra com o histórico nativo do Windows (`Win + V`).
- ⌨️ **Atalho global `Ctrl + \`** — abre e fecha o Colinhas de qualquer lugar, sem tirar a mão do teclado.
- 🔍 **Busca** — filtra o histórico em tempo real por conteúdo ou pela descrição do item.
- 📌 **Fixar** — mantém itens importantes sempre no topo, protegidos da limpeza automática.
- 🙈 **Ocultar** — mascara conteúdos sensíveis (senhas, tokens) sem removê-los.
- 🏷️ **Descrição por item** — dê um título a um item (ex: "API token access") para identificá-lo mesmo quando o conteúdo está oculto.
- 🧩 **Templates de texto** — crie snippets reutilizáveis com placeholders `{campo}`; ao usar, o app pede os valores e copia o resultado pronto.
- 🖱️ **Copiar com um clique** — clique no item e ele já vai para a área de transferência.
- 🔔 **Roda em segundo plano** — vive na bandeja do sistema (system tray), sem ocupar a barra de tarefas.

---

## 🖼️ Screenshots

> _Em breve._ (Adicione imagens em `docs/` e referencie-as aqui.)

---

## 🚀 Como rodar

### Pré-requisitos

- **Windows 10** (build 17763+) ou **Windows 11**
- **[.NET 10 SDK](https://dotnet.microsoft.com/download)**
- (Opcional) **Visual Studio 2022+** com a carga de trabalho *Desenvolvimento para a Plataforma Universal do Windows* / *Windows App SDK*

### Compilar e executar (CLI)

```bash
cd src
dotnet run --project Colinhas.csproj
```

### Visual Studio

Abra `src/Colinhas.slnx`, defina **Colinhas** como projeto de inicialização e pressione **F5**.

---

## ⌨️ Atalho

| Ação | Atalho |
|------|--------|
| Abrir / fechar o Colinhas | `Ctrl + \` |

O atalho é registrado globalmente e funciona em qualquer layout de teclado (US e ABNT2).

---

## 🛠️ Stack

- **[WinUI 3](https://learn.microsoft.com/windows/apps/winui/winui3/)** / **Windows App SDK** — interface
- **[.NET 10](https://dotnet.microsoft.com/)** (C#)
- **[CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)** — padrão MVVM
- **[H.NotifyIcon.WinUI](https://github.com/HavenDV/H.NotifyIcon)** — ícone na bandeja
- **Win32 interop** — `AddClipboardFormatListener` (monitor de clipboard) e `RegisterHotKey` (atalho global)

---

## 📁 Estrutura

```
src/
├── Models/         # ClipboardEntry, TextTemplate
├── ViewModels/     # MainPageViewModel, TemplatesViewModel
├── Services/       # ClipboardMonitor, HotkeyManager, TemplateEngine/Store, Logger
├── Converters/     # BoolToVisibilityConverter
├── Styles/         # Estilos de controles
└── MainPage / MainWindow
```

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para abrir uma [issue](https://github.com/Zaael/colinhas/issues) com ideias, bugs ou sugestões, ou enviar um Pull Request.

---

## 📄 Licença

Distribuído sob a licença **MIT**. Veja [LICENSE](LICENSE) para mais informações.

---

## ☕ Apoie o projeto

O Colinhas é gratuito e open source. Se ele te economizou tempo ou facilitou sua rotina, considere pagar um café — ajuda a manter as ideias saindo do papel!

<a href="https://buymeacoffee.com/zaael" target="_blank">
  <img src="https://img.shields.io/badge/Buy%20Me%20A%20Coffee-apoie%20o%20Colinhas-FFDD00?style=for-the-badge&logo=buymeacoffee&logoColor=black" alt="Buy Me A Coffee" />
</a>
