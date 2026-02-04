# Nouz

[![Build](https://github.com/spflugi/nouz-megatron/actions/workflows/build.yaml/badge.svg)](https://github.com/spflugi/nouz-megatron/actions/workflows/build.yaml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A minimalist, fully local, AI-powered note-taking application built with .NET MAUI and Blazor. Nouz combines the flexibility of block-based editing with intelligent AI assistance to help you capture, organize, and retrieve your thoughts effortlessly.

## Features

### Local, no cloud

- All data is saved locally in a sqlite database

### Block-Based Note Editing

Build notes from flexible, reusable blocks:

- **Text blocks** - Paragraphs, headings (H1-H4), and quotes
- **Lists** - Bullet lists, todo items with checkboxes, and agenda items
- **Code blocks** - Preserve syntax and formatting
- **Special blocks** - Decision boxes, warning boxes, and dividers
- **Images** - Paste or insert images with captions and adjustable sizing

### Rich Text Formatting

- **Inline styles** - Bold, italic, strikethrough, and inline code
- **Text colors** - Apply custom colors with an integrated color picker
- **Floating toolbar** - Context-sensitive formatting when text is selected

### Notebook Organization

- Create multiple notebooks to organize notes by topic or project
- Search notes within a notebook
- Move notes between notebooks
- Clean, distraction-free interface

### AI Assistant

Chat with an intelligent assistant that understands your notes:

- **Context-aware responses** - Uses RAG (Retrieval-Augmented Generation) to search your note library
- **Note creation** - Ask the assistant to create and manage notes from conversations
- **Streaming responses** - Real-time response streaming for a fluid experience
- **Configurable context** - Control how many relevant notes are included (0-10)

### Export Options

- **HTML export** - Self-contained documents with embedded styles and images
- **PDF export** - Convert notes to PDF format
- **Print support** - Optimized CSS for printing

### Appearance

- **Dark/Light mode** - Switch between themes with a single click
- **Minimalist design** - Clean, monochromatic UI with warm tones
- **Resizable sidebars** - Customize your workspace layout

## Screenshots

![Nouz editor with chatbot integration](Screenshots/Showcase.png)

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11 (Windows 10.0.19041.0 or later)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/spflugi/nouz-megatron.git
   cd nouz-megatron
   ```

2. Install the MAUI workload:
   ```bash
   dotnet workload install maui-desktop
   ```

3. Build and run:
   ```bash
   dotnet build Nouz/src/Nouz/Nouz.csproj -c Release -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifierOverride=win10-x64
   ```

### AI Assistant Setup

To enable the AI assistant features:

1. Open Settings in the app
2. Enter your OpenAI API key
3. Configure your preferred chat model (e.g., gpt-4o-mini, gpt-5)
4. Optionally configure the embedding model for semantic search

## Architecture

Nouz follows clean architecture principles with strict layer separation:

```
Nouz (Main MAUI App)           - Blazor UI components
├── Nouz.Application           - Commands, handlers, state management
├── Nouz.Domain                - Entities, repository interfaces
├── Nouz.Infrastructure        - EF Core/SQLite, OpenAI integration
└── Nouz.ReduxSimple           - Custom Redux state management
```

### Tech Stack

| Category | Technologies |
|----------|-------------|
| **Frontend** | .NET MAUI, Blazor WebView, Radzen.Blazor |
| **State Management** | Custom Redux-like pattern with System.Reactive |
| **Database** | Entity Framework Core, SQLite |
| **AI** | OpenAI API, Microsoft Semantic Kernel |
| **Testing** | xUnit, NSubstitute, Shouldly, NetArchTest |

## Development

### Build Commands

```bash
# Build entire solution
dotnet build Nouz/Nouz.slnx

# Build main app (Release)
dotnet build Nouz/src/Nouz/Nouz.csproj -c Release -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifierOverride=win10-x64

# Run all tests
dotnet test Nouz/Nouz.slnx

# Run specific test project
dotnet test Nouz/test/Nouz.Application.Unit.Tests
dotnet test Nouz/test/Nouz.Infrastructure.Unit.Tests
dotnet test Nouz/test/Nouz.Architecture.Tests

# Run single test by name
dotnet test Nouz/Nouz.slnx --filter "FullyQualifiedName~YourTestName"
```

### Project Structure

```
Nouz/
├── src/
│   ├── Nouz/                      # Main MAUI application
│   │   └── Components/            # Blazor UI components
│   ├── Nouz.Application/          # Business logic, CQRS handlers
│   ├── Nouz.Domain/               # Core entities and interfaces
│   ├── Nouz.Infrastructure/       # Data access, external services
│   └── Nouz.ReduxSimple/          # State management library
├── test/
│   ├── Nouz.Application.Unit.Tests/
│   ├── Nouz.Infrastructure.Unit.Tests/
│   └── Nouz.Architecture.Tests/
└── Nouz.slnx                      # Solution file
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Acknowledgments

This project was developed with the assistance of [Claude Code](https://claude.ai/code), Anthropic's AI-powered coding assistant.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
