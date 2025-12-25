# Cloud/SpecSheets Folder

## Purpose

This folder contains specification sheets, catalogs, and reference documents for parts, suppliers, and system components. These documents support offline access to product information and help technicians in the field make informed decisions about parts usage.

## Content Types

### 1. Parts Catalogs

Manufacturer-provided parts catalogs and specification sheets:

```
parts-catalogs/
├── BoltCo-FastenerCatalog-2025.pdf
├── SteelSupply-BeamSpecs-2025.pdf
└── ElectricParts-ComponentGuide-2025.pdf
```

**Usage:** Reference by technicians on mobile devices for part information, dimensions, weight, material specifications.

### 2. Technical Specifications

Detailed technical documents for parts and assemblies:

```
tech-specs/
├── fasteners/
│   ├── M8-bolt-spec.json
│   ├── M10-nut-spec.json
│   └── washer-spec.json
├── electrical/
│   ├── 12v-motor-spec.json
│   └── wiring-gauge-chart.json
└── structural/
    ├── beam-sizing-guide.json
    └── load-ratings.json
```

**Format:** JSON for easy mobile app integration, PDF for archival.

### 3. Supplier Information

Current supplier contact information, pricing, and capabilities:

```
suppliers/
├── BoltCo-contact-info.json
├── SteelSupply-product-list.json
├── ElectricParts-pricing.json
└── supplier-comparison-chart.xlsx
```

### 4. Assembly Instructions

Step-by-step guides for assembling complex parts or systems:

```
assembly-guides/
├── truck-coupler-assembly.pdf
├── hydraulic-system-setup.md
└── electrical-system-wiring.md
```

### 5. Safety Data Sheets (SDS)

Safety information for hazardous materials:

```
safety-data-sheets/
├── lubricant-sds.pdf
├── rust-inhibitor-sds.pdf
└── cleaning-solvent-sds.pdf
```

**Legal Requirement:** Required by OSHA for any hazardous substances.

## Folder Organization

```
Cloud/SpecSheets/
├── parts-catalogs/          # Manufacturer catalogs
├── tech-specs/              # Technical specifications
│   ├── fasteners/
│   ├── electrical/
│   └── structural/
├── suppliers/               # Supplier information
├── assembly-guides/         # Assembly instructions
├── safety-data-sheets/      # SDS documents
├── maintenance-manuals/     # Equipment maintenance
├── pricing/                 # Current pricing information
└── index.json              # Master index of all specs
```

## File Naming Convention

Use clear, descriptive names with version dates:

```
[category]-[subcategory]-[item]-[version-date].extension

Examples:
- parts-catalogs-BoltCo-FastenerCatalog-2025-12-24.pdf
- tech-specs-fasteners-M8-bolt-2025-01-15.json
- suppliers-SteelSupply-pricing-2025-12-24.xlsx
- assembly-guides-truck-coupler-2024-06-01.pdf
```

## JSON Spec Sheet Format

For technical specifications in JSON format:

```json
{
  "spec_id": "spec-M8-bolt",
  "category": "fasteners",
  "name": "M8 Hex Bolt",
  "manufacturer": "BoltCo",
  "version": "1.0",
  "last_updated": "2025-12-24",
  "specifications": {
    "diameter_mm": 8,
    "length_mm": 30,
    "material": "Stainless Steel 304",
    "grade": "8.8",
    "tensile_strength_mpa": 800,
    "yield_strength_mpa": 640,
    "weight_grams": 3.5
  },
  "availability": {
    "in_stock": true,
    "quantity_available": 500,
    "warehouse": "warehouse-A",
    "lead_time_days": 0
  },
  "pricing": {
    "unit_cost_usd": 0.45,
    "bulk_discount_100": 0.40,
    "bulk_discount_1000": 0.35
  },
  "supplier_info": {
    "primary_supplier": "BoltCo",
    "secondary_supplier": "SteelSupply",
    "contact": "sales@boltco.com"
  },
  "notes": "High-strength fastener for structural applications",
  "related_specs": ["spec-M8-nut", "spec-M8-washer"]
}
```

## Master Index

The `index.json` file provides a searchable master index of all spec sheets:

```json
{
  "last_updated": "2025-12-24T22:30:00Z",
  "specs_by_category": {
    "fasteners": [
      {
        "id": "spec-M8-bolt",
        "name": "M8 Hex Bolt",
        "file_path": "tech-specs/fasteners/M8-bolt-spec.json",
        "last_updated": "2025-12-24"
      }
    ],
    "electrical": [
      {
        "id": "spec-12v-motor",
        "name": "12V Electric Motor",
        "file_path": "tech-specs/electrical/12v-motor-spec.json",
        "last_updated": "2025-12-15"
      }
    ]
  },
  "suppliers": [
    {
      "id": "supplier-BoltCo",
      "name": "BoltCo",
      "file_path": "suppliers/BoltCo-contact-info.json"
    }
  ],
  "catalogs": [
    {
      "id": "catalog-BoltCo-2025",
      "name": "BoltCo Fastener Catalog 2025",
      "file_path": "parts-catalogs/BoltCo-FastenerCatalog-2025.pdf"
    }
  ]
}
```

## Access & Sync

This folder is synced to mobile devices via:
- **Primary:** SharePoint: `https://yourorg.sharepoint.com/sites/weirdtoo/Cloud/SpecSheets`
- **Secondary:** Google Drive: `WeirdToo/Cloud/SpecSheets`

Mobile app should:
1. Download entire SpecSheets folder on first app launch
2. Update weekly when new versions are detected
3. Support offline viewing of cached specs
4. Show "last updated" timestamp to warn of potentially stale data

## Maintenance

### Monthly Tasks
- [ ] Review supplier pricing for changes
- [ ] Update SDS sheets if supplier provides new versions
- [ ] Add new parts specifications as they're adopted
- [ ] Archive old versions (keep 2 previous versions)

### Quarterly Tasks
- [ ] Review parts catalogs for discontinued items
- [ ] Update availability information in tech specs
- [ ] Review assembly guides for accuracy
- [ ] Check supplier contact information

### Annually
- [ ] Full catalog review and update
- [ ] Remove obsolete parts from specifications
- [ ] Archive old year's pricing information
- [ ] Update all technical documentation

## Cloud Storage Integration

This folder syncs to cloud storage providers. Configuration in:
- `server/CloudWatcher/auth/appsettings.json` - Provider URLs and credentials

## See Also

- `/Cloud/Requests/` - Device request upload folder
- `/Cloud/Responses/` - Server response folder
- `/Cloud/Archive/` - Historical documents
- `server/database/setup.sql` - Parts and supplier database schema
