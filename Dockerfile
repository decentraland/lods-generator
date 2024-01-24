# prepare base image for TS projects
FROM node:18 as base

RUN apt-get update
RUN apt-get -y -qq install build-essential
RUN npm install -g yarn

ENV TINI_VERSION v0.19.0
ADD https://github.com/krallin/tini/releases/download/${TINI_VERSION}/tini /tini
RUN chmod +x /tini

# build scene-lod
FROM base as scene-lod-build

WORKDIR /scene-lod

RUN npm cache clean --force
COPY scene-lod-entities-manifest-builder/package.json /scene-lod
COPY scene-lod-entities-manifest-builder/package-lock.json /scene-lod
RUN npm ci

COPY scene-lod-entities-manifest-builder /scene-lod

RUN npm run build

# build consumer-server
FROM base as consumer-server-build

WORKDIR /consumer-server

COPY consumer-server/package.json /consumer-server/package.json
COPY consumer-server/yarn.lock /consumer-server/yarn.lock
RUN yarn install --frozen-lockfile

COPY consumer-server /consumer-server
RUN yarn build

#build the dotnet app
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as dotnet-build

WORKDIR /build

COPY DCL_PiXYZ/ ./DCL_PiXYZ
COPY nuget.config ./
COPY local_packages/ ./local_packages
COPY PiXYZ.sln ./

RUN dotnet publish -c Release -r win10-x64 -o ./publish --self-contained true
ARG VULKAN_DLL_PATH
COPY ${VULKAN_DLL_PATH} ./publish/vulkan-1.dll

# bundle all apps
FROM unityci/editor:ubuntu-2023.2.6f1-windows-mono-3.0.1

RUN    apt-get update -y \
    && apt-get -y install \
    xvfb \
    && rm -rf /var/lib/apt/lists/* /var/cache/apt/*

ENV NVM_DIR /root/.nvm
ENV NODE_VERSION v18.14.2

RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.2/install.sh | bash
RUN /bin/bash -c "source $NVM_DIR/nvm.sh && nvm install $NODE_VERSION && nvm use --delete-prefix $NODE_VERSION"

ENV NODE_PATH $NVM_DIR/versions/node/$NODE_VERSION/lib/node_modules
ENV PATH $NVM_DIR/versions/node/$NODE_VERSION/bin:$PATH
ENV NODE_ENV production

WORKDIR /vulkan-sdt
ARG VULKAN_DLL_PATH
COPY ${VULKAN_DLL_PATH} .

# set path on linux to discover /vulkan-sdt dlls
ENV LD_LIBRARY_PATH=/vulkan-sdt

WORKDIR /app/api
COPY --from=dotnet-build /build/publish/ .

WORKDIR /app

COPY --from=base /tini /tini

COPY ./pixyzsdk-29022024.lic ./pixyzsdk-29022024.lic
COPY --from=scene-lod-build /scene-lod/dist ./scene-lod/dist
COPY --from=scene-lod-build /scene-lod/package.json ./scene-lod/package.json
COPY --from=scene-lod-build /scene-lod/package-lock.json ./scene-lod/package-lock.json
COPY --from=scene-lod-build /scene-lod/node_modules ./scene-lod/node_modules

COPY --from=consumer-server-build /consumer-server/dist ./consumer-server/dist
COPY --from=consumer-server-build /consumer-server/package.json ./consumer-server/package.json
COPY --from=consumer-server-build /consumer-server/yarn.lock ./consumer-server/yarn.lock
COPY --from=consumer-server-build /consumer-server/node_modules ./consumer-server/node_modules

# Please _DO NOT_ use a custom ENTRYPOINT because it may prevent signals
# (i.e. SIGTERM) to reach the service
# Read more here: https://aws.amazon.com/blogs/containers/graceful-shutdowns-with-ecs/
#            and: https://www.ctl.io/developers/blog/post/gracefully-stopping-docker-containers/
ENTRYPOINT ["/tini", "-g", "--", "xvfb-run", "--auto-servernum", "--error-file", "/dev/stdout" ]

# Run the program under Tini+xvfb
CMD [ "node", "--trace-warnings", "--abort-on-uncaught-exception", "--unhandled-rejections=strict", "./consumer-server/dist/index.js" ]