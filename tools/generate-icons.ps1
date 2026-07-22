<#
.SYNOPSIS
    Gera todos os ícones do Colinhas a partir da geometria do logo do coala.

.DESCRIPTION
    O logo original está em /logo (SVG + PNGs). Este script NÃO redimensiona
    aqueles PNGs: ele redesenha o coala vetorialmente em cada tamanho, com
    GDI+, por dois motivos:

      1. Reduzir um PNG de 1024px para 16px borra tudo. O ícone da bandeja é o
         que o usuário mais vê neste app, e a 16px ele precisa ser desenhado,
         não espremido.
      2. Abaixo de 32px os detalhes internos (orelha interna, olhos) viram
         sujeira. Nesses tamanhos o script usa uma variante "compacta": só
         cabeça, orelhas e focinho, e com menos margem, para a silhueta ocupar
         mais pixels.

    A geometria é a mesma do logo/koala-primary.svg, então o desenho continua
    fiel à arte original.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\generate-icons.ps1
#>
param(
    [string]$AssetsDir = (Join-Path $PSScriptRoot '..\src\Assets')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class KoalaIcon
{
    // Paleta da marca (logo/README.txt).
    public static readonly Color Brand  = ColorTranslator.FromHtml("#6C9A7A");
    public static readonly Color Fur    = ColorTranslator.FromHtml("#F7F4EC");

    // Caixa que envolve o desenho no sistema de coordenadas do SVG original:
    // orelhas em cima (y=33) e focinho embaixo (y=164).
    const float BX = 21f, BY = 33f, BW = 158f, BH = 131f;

    // ---------- primitivas ----------

    static void Circle(Graphics g, Brush b, float cx, float cy, float r)
    {
        g.FillEllipse(b, cx - r, cy - r, r * 2f, r * 2f);
    }

    static GraphicsPath NosePath()
    {
        GraphicsPath p = new GraphicsPath();
        p.AddBezier(100, 104,  89, 104,  83, 111,  81, 121);
        p.AddBezier( 81, 121,  79, 131,  76, 136,  76, 142);
        p.AddBezier( 76, 142,  76, 153,  87, 159, 100, 159);
        p.AddBezier(100, 159, 113, 159, 124, 153, 124, 142);
        p.AddBezier(124, 142, 124, 136, 121, 131, 119, 121);
        p.AddBezier(119, 121, 117, 111, 111, 104, 100, 104);
        p.CloseFigure();
        return p;
    }

    /// <summary>Desenha o coala dentro de box. compact = versão para tamanhos pequenos.</summary>
    static void DrawKoala(Graphics g, RectangleF box, Color fur, Color detail, bool compact)
    {
        float scale = Math.Min(box.Width / BW, box.Height / BH);
        float ox = box.X + (box.Width  - BW * scale) / 2f - BX * scale;
        float oy = box.Y + (box.Height - BH * scale) / 2f - BY * scale;

        GraphicsState st = g.Save();
        g.TranslateTransform(ox, oy);
        g.ScaleTransform(scale, scale);

        using (Brush f = new SolidBrush(fur))
        using (Brush d = new SolidBrush(detail))
        {
            Circle(g, f,  54, 66, 33);   // orelha esquerda
            Circle(g, f, 146, 66, 33);   // orelha direita

            if (!compact)
            {
                Circle(g, d,  54, 69, 16);
                Circle(g, d, 146, 69, 16);
            }

            g.FillEllipse(f, 100 - 56, 112 - 52, 112, 104);  // cabeça

            using (GraphicsPath nose = NosePath())
                g.FillPath(d, nose);

            if (!compact)
            {
                Circle(g, d,  77, 93, 7);
                Circle(g, d, 123, 93, 7);
            }
        }

        g.Restore(st);
    }

    static Graphics Quality(Bitmap bmp)
    {
        Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        return g;
    }

    /// <summary>Renderiza em 4x e reduz — bordas bem mais limpas nos tamanhos pequenos.</summary>
    static Bitmap Downsample(Bitmap big, int w, int h)
    {
        Bitmap outp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (Graphics g = Quality(outp))
            g.DrawImage(big, new Rectangle(0, 0, w, h));
        big.Dispose();
        return outp;
    }

    // ---------- variantes ----------

    /// <summary>Ícone "com plaquinha": quadrado arredondado da marca + coala claro.</summary>
    public static Bitmap Plated(int size)
    {
        int ss = size <= 256 ? 4 : 1;
        int s = size * ss;

        Bitmap bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (Graphics g = Quality(bmp))
        {
            float r = s * 0.2227f;   // mesmo raio do koala-appicon.svg (114/512)
            using (GraphicsPath plate = new GraphicsPath())
            {
                plate.AddArc(0, 0, r * 2, r * 2, 180, 90);
                plate.AddArc(s - r * 2, 0, r * 2, r * 2, 270, 90);
                plate.AddArc(s - r * 2, s - r * 2, r * 2, r * 2, 0, 90);
                plate.AddArc(0, s - r * 2, r * 2, r * 2, 90, 90);
                plate.CloseFigure();
                using (Brush b = new SolidBrush(Brand))
                    g.FillPath(b, plate);
            }

            // Até 36px o desenho perde os detalhes finos e ganha área: a partir
            // de ~40px a orelha interna e os olhos já têm pixels suficientes
            // para ajudar em vez de virar ruído.
            bool compact = size < 40;
            float pad = compact ? 0.10f : 0.15f;
            RectangleF box = new RectangleF(s * pad, s * pad, s * (1 - 2 * pad), s * (1 - 2 * pad));
            DrawKoala(g, box, Fur, Brand, compact);
        }

        return ss == 1 ? bmp : Downsample(bmp, size, size);
    }

    /// <summary>Tile/splash: fundo da marca sangrando até a borda + coala centralizado.</summary>
    public static Bitmap Tile(int w, int h, float occupancy)
    {
        int ss = (w <= 256 && h <= 256) ? 4 : 1;
        int sw = w * ss, sh = h * ss;

        Bitmap bmp = new Bitmap(sw, sh, PixelFormat.Format32bppArgb);
        using (Graphics g = Quality(bmp))
        {
            using (Brush b = new SolidBrush(Brand))
                g.FillRectangle(b, 0, 0, sw, sh);

            float side = Math.Min(sw, sh) * occupancy;
            RectangleF box = new RectangleF((sw - side) / 2f, (sh - side) / 2f, side, side);
            DrawKoala(g, box, Fur, Brand, false);
        }

        return ss == 1 ? bmp : Downsample(bmp, w, h);
    }

    /// <summary>Tela de bloqueio: o Windows exige monocromático branco sobre transparente.</summary>
    public static Bitmap Monochrome(int size)
    {
        int s = size * 4;
        Bitmap bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (Graphics g = Quality(bmp))
        {
            RectangleF box = new RectangleF(0, 0, s, s);
            DrawKoala(g, box, Color.White, Color.Transparent, true);
        }
        return Downsample(bmp, size, size);
    }

    // ---------- saída ----------

    public static void SavePng(Bitmap bmp, string path)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        bmp.Save(path, ImageFormat.Png);
        bmp.Dispose();
    }

    /// <summary>
    /// Escreve um .ico multi-resolução com os quadros em PNG (suportado pelo
    /// Windows desde o Vista). É o ícone da bandeja e da barra de título.
    /// </summary>
    public static void SaveIco(string path, int[] sizes)
    {
        List<byte[]> frames = new List<byte[]>();
        foreach (int size in sizes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (Bitmap bmp = Plated(size))
                    bmp.Save(ms, ImageFormat.Png);
                frames.Add(ms.ToArray());
            }
        }

        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter w = new BinaryWriter(fs))
        {
            w.Write((short)0);               // reservado
            w.Write((short)1);               // tipo: ícone
            w.Write((short)sizes.Length);

            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                byte dim = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
                w.Write(dim);                // largura
                w.Write(dim);                // altura
                w.Write((byte)0);            // paleta
                w.Write((byte)0);            // reservado
                w.Write((short)1);           // planos
                w.Write((short)32);          // bits por pixel
                w.Write(frames[i].Length);
                w.Write(offset);
                offset += frames[i].Length;
            }

            foreach (byte[] frame in frames)
                w.Write(frame);
        }
    }
}
'@

$AssetsDir = [System.IO.Path]::GetFullPath($AssetsDir)
if (-not (Test-Path $AssetsDir)) { New-Item -ItemType Directory -Path $AssetsDir | Out-Null }
Write-Host "Gerando ícones em $AssetsDir"

function Save-Plated([int]$Size, [string]$Name) {
    [KoalaIcon]::SavePng([KoalaIcon]::Plated($Size), (Join-Path $AssetsDir $Name))
}

function Save-Tile([int]$W, [int]$H, [double]$Occupancy, [string]$Name) {
    [KoalaIcon]::SavePng([KoalaIcon]::Tile($W, $H, $Occupancy), (Join-Path $AssetsDir $Name))
}

# Escalas de DPI que o Windows procura para cada asset.
$scales = @(100, 125, 150, 200, 400)

# --- Square44x44Logo: barra de tarefas, lista de aplicativos, bandeja ---
foreach ($s in $scales) {
    Save-Plated ([math]::Round(44 * $s / 100)) "Square44x44Logo.scale-$s.png"
}

# targetsize: tamanhos exatos, sem escala de DPI. As variantes unplated são
# usadas na barra de tarefas; como o nosso ícone já traz o próprio fundo, elas
# são iguais à versão normal e funcionam em tema claro e escuro.
foreach ($t in @(16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256)) {
    Save-Plated $t "Square44x44Logo.targetsize-$t.png"
    Save-Plated $t "Square44x44Logo.targetsize-${t}_altform-unplated.png"
    Save-Plated $t "Square44x44Logo.targetsize-${t}_altform-lightunplated.png"
}

# --- Tiles ---
foreach ($s in $scales) {
    Save-Tile ([math]::Round(71  * $s / 100)) ([math]::Round(71  * $s / 100)) 0.62 "Square71x71Logo.scale-$s.png"
    Save-Tile ([math]::Round(150 * $s / 100)) ([math]::Round(150 * $s / 100)) 0.58 "Square150x150Logo.scale-$s.png"
    Save-Tile ([math]::Round(310 * $s / 100)) ([math]::Round(310 * $s / 100)) 0.55 "Square310x310Logo.scale-$s.png"
    Save-Tile ([math]::Round(310 * $s / 100)) ([math]::Round(150 * $s / 100)) 0.62 "Wide310x150Logo.scale-$s.png"
    Save-Tile ([math]::Round(620 * $s / 100)) ([math]::Round(300 * $s / 100)) 0.42 "SplashScreen.scale-$s.png"
    Save-Plated ([math]::Round(50 * $s / 100)) "StoreLogo.scale-$s.png"
}

# --- Tela de bloqueio (monocromático) ---
[KoalaIcon]::SavePng([KoalaIcon]::Monochrome(24), (Join-Path $AssetsDir 'LockScreenLogo.scale-200.png'))

# --- Ícone da janela e da bandeja ---
[KoalaIcon]::SaveIco((Join-Path $AssetsDir 'AppIcon.ico'), @(16, 20, 24, 32, 48, 64, 128, 256))

# Placeholders do template que perderam a função (agora existem como .scale-*).
foreach ($old in @('StoreLogo.png')) {
    $p = Join-Path $AssetsDir $old
    if (Test-Path $p) { Remove-Item $p; Write-Host "removido placeholder: $old" }
}

$count = (Get-ChildItem $AssetsDir -File).Count
Write-Host "Pronto — $count arquivos em Assets."
