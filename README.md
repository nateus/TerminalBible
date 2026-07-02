# Terminal Bible

Leitor bíblico leve para terminal, escrito em C#/.NET. A primeira versão baixa a Bíblia Portuguesa Mundial do eBible, importa arquivos USFM e mantém uma cópia JSON local para leitura offline.

## Requisitos

- .NET SDK 8 ou superior
- Conexão com internet apenas para a instalação/atualização inicial da Bíblia

## Rodar

```powershell
dotnet run --project src/TerminalBible
```

No primeiro uso, escolha baixar a Bíblia Portuguesa Mundial. Depois disso, a leitura funciona offline.

## Testar

```powershell
dotnet test
```

## Dados offline

Os arquivos importados são salvos em:

```text
%LocalAppData%\TerminalBible\bibles\porbrbsl
```

Em outros sistemas, a pasta base vem de `Environment.SpecialFolder.LocalApplicationData`.
