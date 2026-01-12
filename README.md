# ScryForge

ScryForge is an automated pipeline for downloading, upscaling, and processing Magic: The Gathering cards. It generates PDF files from your cards and fully supports double-faced and flip cards, including custom cards.

## Features

- Automatically download card art via an external downloader.
- Upscale cards for high-quality output (only if cards are present).
- Supports double-faced and flip cards.
- Handles custom cards from a `Custom` folder.
- Copies, duplicates, and organizes card files.
  - Custom cards are copied directly to the output folders.
  - Flip cards are copied to a dedicated `flips` folder.
- Generates PDF files (`default.pdf` and `flips.pdf`) from the cards.
- Automatic cleanup of temporary and upscaled files.
- Easily opens the output folder after processing.
- Console logging with timestamps and clear status updates.

### Preparing `cards.txt`

To use ScryForge, open your deck in Moxfield and export it as a text file. Copy the exported list into `cards.txt`. The pipeline will read this file to download and process your cards.

### Custom Cards

- Place any custom card images (`.jpg` or `.png`) in the `Custom` folder in the application directory.
- Flip cards can have a back image with the prefix `__back_`.
  - **Important:** The filename after `__back_` must **exactly match** the corresponding front card filename, including the extension.
  - ScryForge will automatically pair front and back images based on this naming convention.

## Image Upscaling Integration

ScryForge now includes built-in support for automated batch upscaling of card images through an external upscaler executable (for example, Real-ESRGAN or similar tools).  
This feature allows you to upscale all card images generated during the download process before they are used for PDF creation.

### How It Works

When enabled, ScryForge performs the following steps automatically:

1. **Identifies all images belonging to the selected cards**  
   Only files referenced by the cards’ `ImagePath` (or per-face image properties like `FrontImagePath` and `BackImagePath`) are considered for upscaling.

2. **Temporarily isolates these images**  
   - All non-card images inside `AppConfig.ScryForgeDownloaderPath` are temporarily moved to a `.temp` subfolder.  
   - Only the actual card image files remain in the working directory for processing.

3. **Runs the external upscaler**  
   ScryForge invokes the tool defined in `AppConfig.UpscalerExe` with the configured model and scale.  
   Output is written to `AppConfig.PDFImagesFolder`.

4. **Restores the original folder contents**  
   After upscaling completes, everything inside the temporary folder is moved back to `ScryForgeDownloaderPath`.

5. **Progress reporting**  
   The system parses the upscaler's stdout/stderr and matches progress lines to specific cards so that progress can be displayed accurately.

---

### Default Configuration

By default, ScryForge uses the following upscaler setup:

| Upscaler Name        | Model Name         | Scale | Year Range      | Description |
|---------------------|-----------------|-------|----------------|-------------|
| Uniscale Restore     | `uniscale-restore` | 4     | From: null / To: 2009 | Used for older cards (before 2010). Best for restoring lines and color on old Magic cards. |
| Digital Art 4x       | `digital-art-4x`  | 4     | From: 2010 / To: null | Used for modern cards. Provides stylized, high-fidelity upscaling for newer illustrations. |

This ensures that:

- Cards released **up to and including 2009** are processed with Uniscale Restore for gentle restoration.  
- Cards released **from 2010 onwards** are processed with Digital Art 4x for modern, high-fidelity results.  

---

### Configuration

Upscalers can be customized in `appsettings.json` under the `Upscalers` section. Example:

```json
{
  "Upscalers": [
    {
      "Name": "Uniscale Restore",
      "Model": "uniscale-restore",
      "Scale": 4,
      "YearRange": { "From": null, "To": 2009 }
    },
    {
      "Name": "Digital Art 4x",
      "Model": "digital-art-4x",
      "Scale": 4,
      "YearRange": { "From": 2010, "To": null }
    }
  ]
}
```

#### Setting Descriptions

| Field          | Type    | Description |
|----------------|--------|-------------|
| `Name`         | string | Logical name of the upscaler profile. |
| `Model`        | string | The model name passed to the external upscaler executable. |
| `Scale`        | int    | Upscale factor (e.g., 2 or 4). |
| `YearRange.From` | int?  | Start year (inclusive). Use `null` for “from the beginning”. |
| `YearRange.To`   | int?  | End year (inclusive). Use `null` for “up to indefinite”. |

---

### Upscaler Invocation

ScryForge generates a command similar to the following:

```bash
upscaler.exe -i "<ScryForgeDownloaderPath>" -o "<PDFImagesFolder>" -n <model> -s <scale> -v
```

The tool runs asynchronously, and ScryForge merges stdout/stderr events to provide detailed progress logging.

---

This automated workflow ensures fast, clean, and isolated image processing without accidentally upscaling unrelated files.



## Project Structure

- **Services**
  - `CleanupService` – Removes files and folders that are no longer needed.
  - `OpenFolderService` – Opens a folder in Windows Explorer.
  - `CardParserService` – Parses `cards.txt` and custom cards into internal card info format.
  - `DownloaderService` – Starts the art downloader and waits for it to finish.
  - `UpscalerService` – Upscales the downloaded card images (skipped if no cards present).
  - `CustomCardService` – Loads and copies custom card images to the output folders.
  - `CopyService` – Copies and duplicates card files for PDF generation.
  - `FlipService` – Processes double-faced flip cards (skips custom cards).
  - `PDFService` – Generates PDF files from the cards.
  - `PDFOpenService` – (optional) opens PDF files.
  - `PipelineService` – Coordinates all pipeline steps in the correct order.

- **Models**
  - `CardInfo` – Holds information about a card (name, set, quantity, front/back file).
  - `CustomCard` – Represents a custom card with optional back image.

- **Configuration**
  - `AppConfig` – Contains paths and file locations for cards, downloads, custom cards, flips, and PDF output.

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/ScryForge.git
   cd ScryForge


