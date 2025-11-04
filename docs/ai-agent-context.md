# AI Agent Context

**Service Purpose:** Generates Level of Detail (LOD) models for Decentraland scene entities. Uses PiXYZ plugin integration to create optimized LOD versions of 3D assets for performance optimization, reducing polygon count and texture resolution for distant viewing.

**Key Capabilities:**

- Consumes deployment events from SQS queue (scenes requiring LOD generation)
- Generates multiple LOD levels (LOD0, LOD1, LOD2) with decreasing detail
- Uses PiXYZ plugin for automated LOD generation and optimization
- Uploads generated LODs to S3/CDN for client retrieval
- Publishes completion events to SNS for downstream services (Asset Bundle Registry)
- Supports platform-specific LOD variants (Windows, Mac, WebGL)

**Communication Pattern:** 
- Event-driven via AWS SQS (consumes deployment events)
- Publishes completion events to AWS SNS

**Technology Stack:**

- **LOD Generation**: PiXYZ plugin (3D optimization tool)
- **Service Runtime**: Node.js (consumer-server)
- **Language**: TypeScript (service), potentially Unity/PiXYZ scripts
- **Storage**: AWS S3 (LOD model storage)
- **Queue/Events**: AWS SQS (deployment events), AWS SNS (completion events)

**External Dependencies:**

- Queue: AWS SQS (`lods-queue` subscribed to deployments SNS topic)
- Storage: AWS S3 (LOD model storage, organized by platform)
- Event Bus: AWS SNS (publishes `LODsGenerationFinished` events)
- Content Server: Catalyst (fetches entity content for LOD generation)
- Optimization Tool: PiXYZ plugin (automated LOD generation)

**Workflow:**

1. Deployment event received from SQS
2. Entity content fetched from Catalyst
3. PiXYZ LOD generation process initiated (multiple LOD levels)
4. Optimized LOD models generated and validated
5. LODs uploaded to platform-specific S3 paths
6. Completion event published to SNS (consumed by Asset Bundle Registry)
