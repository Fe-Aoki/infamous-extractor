# Componentes de terceiros

O código original da interface e integração é disponibilizado sob **GPL-3.0-or-later**. Este aviso não substitui as licenças das dependências.

- **LibOrbisPkg**, Maxton e colaboradores: LGPL-3.0; base `643477263b2644e0803e0f58b8726ea4e3f3b7d4`. Fontes completos em `vendor/LibOrbisPkg`; licença original preservada. Modificação local em `LibOrbisPkg/PFS/PFSCReader.cs`: leitura integral de setores descomprimidos. Patch em `licencas/Correcao-extrator.patch`.
- **7-Zip 26.04**, Igor Pavlov: licença em `tools/7zip/LICENSE-7zip.txt`, que inclui LGPL, avisos BSD e a restrição de uso do código unRAR. Os binários não foram modificados. Fonte oficial correspondente em `vendor/7z2604-src.tar.xz`, obtido de `https://www.7-zip.org/a/7z2604-src.tar.xz`.
- **Barlow** e **Big Shoulders Stencil Display**: SIL OFL; avisos originais em `licencas/OFL-*.txt`.
- **iss_pc**, HayatoG e colaboradores: referência de paleta/identidade visual e origem das fontes de interface usadas como referência. Não é uma dependência de execução deste preparador. https://github.com/HayatoG/iss_pc
- **Ícone de extração**: gerado com a ferramenta image_gen integrada a partir de uma referência fornecida pelo usuário; prompt e origem documentados em `assets/ICON-PROMPT.txt`. Não inclui `pic1.png` do jogo.

O nome inFAMOUS é usado para identificar o jogo compatível. O projeto é independente, sem afiliação ou endosso dos detentores da marca.
