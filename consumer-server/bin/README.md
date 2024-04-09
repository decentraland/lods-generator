# Scripts Overview

This document offers an overview of the scripts we've developed to manage and utilize external resources for LOD (Level of Detail).

## ZIP Generation Tool

Found at `/consumer-server/bin/generate-zip.ts`, this tool is essential for preparing ZIP files that are integrated into Explorer builds. This process enables LOD to be rendered directly in Explorer without the need to stream them from a content delivery network.

### What This Tool Does:

- It creates compressed files tailored for inclusion in the builds of Explorer.
- It generates a distinct ZIP file for each combination of platform and LOD level.

### Setting Up for Use:

To use this tool, you'll need to provide credentials for accessing the LOD storage bucket on AWS. This is done by creating a `.env.admin` file in the `consumer-server` directory, which must include:
- `AWS_USER`: The key for the AWS programmatic user.
- `AWS_SECRET`: The secret key for the AWS programmatic user.
- `S3_BUCKET`: The name of the bucket where LOD files are stored (referred to as Asset Bundle CDN).

### How to Run the Tool:

1. Ensure NodeJS version 18+ and YARN package manager are installed.
2. Navigate to the `consumer-server` directory.
3. Install the project's dependencies by executing `yarn install`.
4. Build the scripts with `yarn build:admin`.
5. Based on your specific requirements, run one of the following commands:
    - `yarn admin:lod0` to generate two compressed files (for each platform) containing all generated LOD 0 files.
    - `yarn admin:lod1` for generating similar files for LOD 1.
    - `yarn admin:lod2` for LOD 2 files.
    - `yarn admin:lod3` for LOD 3 files.

## License Building Tool

This section introduces a script designed for securely build the PiXYZ software license. The core functionality of this script revolves around retrieving a secure token, which represents the software's license, from an environmental variable named `LODS_GENERATOR_LICENSE`. The script then proceeds to compile this information into the formatted license expected for use.

### Purpose of the Tool:

- Securely generate the license needed to operate the PiXYZ software by leveraging a secure token.

### How It Works:

The script achieves its goal by accessing a specific environmental variable:

- `LODS_GENERATOR_LICENSE`: This variable stores the secure token necessary for generating the PiXYZ software license. Should be stored as a secret.

Alternatively, you can provide the license key as an argument when executing the command.

### Execution Guide:

This tool is straightforward to use, focusing on security and efficiency in license generation for PiXYZ software. It ensures that the licensing process is both secure and compliant with the required standards.

1. Ensure NodeJS version 18+ and YARN package manager are installed.
2. Navigate to the `consumer-server` directory.
3. Install the project's dependencies by executing `yarn install`.
4. Build the scripts with `yarn build:admin`.
5. Run the script `yarn admin:license`
