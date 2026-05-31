# Examples

This directory is for example blockout images and palette overrides.

---

## Getting started

The fastest way to see PZMapForge in action is the built-in sample image:

```powershell
cd E:\Omni\Zomboid\PZMapForge
powershell -ExecutionPolicy Bypass -File "scripts\validate.ps1"
```

This generates `.local\mapforge\sample-input.png` (a 300x300 image using the
default palette colours), runs ImageMapForge against it, and verifies all outputs.

---

## Creating your own blockout image

1. Paint a 300x300 PNG using the exact RGB colours from `source/image-palette.json`.

   | Kind | RGB |
   |---|---|
   | grass | 100, 140, 70 |
   | road | 70, 70, 70 |
   | sidewalk | 190, 180, 160 |
   | row_house | 160, 110, 80 |
   | depanneur | 200, 130, 60 |
   | garage | 80, 80, 100 |
   | industrial_yard | 160, 130, 90 |
   | landmark | 255, 220, 0 |
   | spawn | 0, 220, 80 |

2. Save it to `.local\mapforge\mymap.png`.

3. Run:
   ```powershell
   powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath ".local\mapforge\mymap.png"
   ```

4. Check `.local\mapforge\parsed-cell-report.md` for the drift table. High
   drift distances mean your paint colours do not closely match the palette.

---

## Non-300x300 input

If your image is not 300x300, pass `-Resize`:

```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" `
    -ImagePath "largemap.png" -Resize
```

The image is scaled to 300x300 using nearest-neighbour sampling before parsing.

---

## Debug mode

Run `-Mode Debug` first to see colour frequencies without generating any artifacts:

```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" `
    -ImagePath ".local\mapforge\mymap.png" -Mode Debug
```

The output shows each unique colour and how many pixels used it, plus a list of
colours that do not exactly match the palette. Use this to identify paint errors
before committing to a full run.

---

## Using PZMapForge from another repo

If you are calling PZMapForge from a different map mod directory, use
`-AllowExternalOutput` and point `-OutputDir` at a `.local/` path in your mod repo:

```powershell
powershell -ExecutionPolicy Bypass -File "E:\Omni\Zomboid\PZMapForge\source\image-mapforge.ps1" `
    -ImagePath "E:\Omni\Zomboid\my-mod\.local\cellforge\canal-garage-cell-blockout.png" `
    -Resize `
    -OutputDir "E:\Omni\Zomboid\my-mod\.local\mapforge" `
    -AllowExternalOutput
```

The `-Resize` flag handles non-300x300 input (the CellForge blockout PNG is 900x900).
