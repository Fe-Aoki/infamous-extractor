# Automação opcional

A interface continua sendo o modo normal de uso. Para integração local, o mesmo motor pode ser chamado pelo executável:

```powershell
.\Preparador-inFAMOUS.exe --prepare "C:\Arquivos\local.job.xml" "C:\Arquivos\preparacao.log"
```

Exemplo de `local.job.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Job>
  <Base>C:\Arquivos\jogo.part1.rar</Base>
  <BaseFiles>
    <string>C:\Arquivos\jogo.part1.rar</string>
    <string>C:\Arquivos\jogo.part2.rar</string>
    <string>C:\Arquivos\jogo.part3.rar</string>
  </BaseFiles>
  <Update>C:\Arquivos\update.rar</Update>
  <Destination>C:\Jogos\Game-CUSA00223</Destination>
  <BasePassword></BasePassword>
  <UpdatePassword></UpdatePassword>
</Job>
```

O XML pode conter senhas dos arquivos: mantenha arquivos `*.job.xml` locais, fora do controle de versão. O aplicativo não baixa os arquivos. O log contém `PREPARE_COMPLETE` em caso de sucesso e `PREPARE_FAILED` em caso de erro. O código de saída é 0 ou 1, respectivamente.

## RAR5 armazenado

Quando todos os volumes contêm um único PKG não comprimido e não criptografado no formato RAR5, o aplicativo pode ler o PKG diretamente, sem criar uma cópia intermediária. A integridade do conjunto é verificada pelo 7-Zip antes de extrair os arquivos do jogo. Outros formatos continuam usando a descompactação normal.

Os arquivos de entrada continuam preservados pelo programa.

## Marcadores de integração

Os campos abaixo são opcionais e foram usados no teste local com pouco espaço. Não são necessários na interface:

- `BufferLastRarVolume=true`: copia o último volume pequeno para a RAM, até o limite de 1.280 MiB, mantendo os arquivos de entrada intactos.
- `BufferedVolumeReleaseMarker`: depois de informar `VOLUME_BUFFERED:<caminho>`, aguarda este arquivo de sinalização. Uma ferramenta externa pode usá-lo para coordenar o espaço de temporários que ela mesma gerencia.
- `UpdateReadyMarker`: permite preparar a base antes de o update estar disponível. Depois de `BASE_STAGE_COMPLETE`, aguarda esse sinal para abrir o update.

Não configure marcadores sem um controlador externo que os produza; a preparação ficará aguardando. Esses campos não apagam entradas. A remoção de downloads descartáveis no teste completo foi feita pelo controlador local, sob autorização do usuário, e não foi incorporada à interface pública.
