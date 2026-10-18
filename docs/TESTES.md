# Verificação da versão 0.6

Em 18/10/2026, o executável foi recompilado a partir dos fontes deste repositório e passou por 15 verificações funcionais:

1. Rejeição de caminhos inseguros.
2. Identificação da primeira parte RAR.
3. Agrupamento de três partes RAR.
4. Extração de ZIPs base/update, união, limpeza e preservação de entradas.
5. Proteção de destino existente.
6. União de três ZIPs independentes.
7. Cancelamento sem publicação de instalação parcial.
8. Extração de PKGs sintéticos e metadados.
9. PKG direto de três RAR5 armazenados, CRC e buffer em RAM, preservando volumes.
10. ZIP dividido com senha contendo espaços, aspas e barra.
11. Senha incorreta.
12. ZIP com tentativa de escapar da pasta de extração.
13. Extração de amostra RAR5.
14. Rejeição de RAR com link simbólico.
15. Renderização da interface.

Resultado textual em VALIDACAO.txt. O manifesto e a configuração Windows permitem caminhos longos no computador de teste, com a política do sistema já habilitada. Não foi necessário mudar essa política.

## Teste integral

Uma nova extração foi executada com três volumes RAR5 da base e um RAR de update 1.07. O executável foi chamado pelo modo --prepare, que usa o mesmo motor do botão da interface. Foram extraídos 1.827 arquivos da base e 458 do update; após a união, 1.847 arquivos foram comparados por SHA-256 com a instalação local anteriormente funcional. Todos foram idênticos: nenhum ausente ou diferente. PREPARACAO.txt é um relatório adicional gerado pelo aplicativo.

O programa usa leitura direta para este conjunto RAR5 armazenado. Um controlador local gerenciou os downloads e apagou seus próprios RARs descartáveis sob autorização do usuário, coordenando o espaço por marcadores e buffer opcional. Esse controlador e os arquivos do jogo não fazem parte do repositório. A interface pública preserva as entradas e não baixa jogos.

A comparação completa foi uma validação externa, não um recurso automático do aplicativo. A referência local não é um manifesto oficial. O teste integral não simula cliques manuais no seletor de arquivos; a interface e a seleção foram cobertas pelos casos pequenos.

## Amostras opcionais

As amostras RAR usadas localmente vieram de:

- https://github.com/markokr/rarfile/blob/master/test/files/rar5-crc.rar
- https://github.com/libarchive/libarchive/blob/master/libarchive/test/test_read_format_rar.rar.uu

Elas não estão incluídas. Sem essas amostras, o autoteste informa os dois casos como ausentes. O caso de três RAR5 armazenados é gerado localmente e não depende delas.

## Histórico e limites

A versão 0.5 foi extraída do próprio ZIP, teve 147 hashes de pacote conferidos, passou por 14 verificações e foi recompilada a partir dos fontes incluídos. A revisão 0.6 adicionou caminhos longos e leitura direta de RAR5, depois recebeu o teste integral descrito acima.

O isolamento do agente bloqueou uma movimentação de pasta no primeiro teste; o reteste fora do isolamento passou. Isso não estabelece exigência de executar o programa como administrador. Estes resultados não garantem suporte a outros PKGs, funcionamento em qualquer computador ou desempenho do jogo.
