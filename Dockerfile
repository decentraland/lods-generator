# prepare base image for TS projects
FROM mcr.microsoft.com/windows/servercore:ltsc2019 as base

ADD https://aka.ms/vs/16/release/vc_redist.x64.exe C:\\vc_redist.x64.exe
RUN C:\\vc_redist.x64.exe /quiet /install

ADD https://nodejs.org/dist/v18.0.0/node-v18.0.0-win-x64.zip C:\\node.zip
RUN powershell -Command \
    Expand-Archive -Path C:\\node.zip -DestinationPath C:\\Node; \
    Remove-Item -Force C:\\node.zip

RUN setx /M PATH "C:\Node\node-v18.0.0-win-x64;%PATH%"

ADD https://classic.yarnpkg.com/latest.msi C:\\yarn.msi
RUN powershell -Command \
    Start-Process msiexec.exe -ArgumentList '/i', 'C:\\yarn.msi', '/quiet', '/norestart' -NoNewWindow -Wait; \
    Remove-Item -Force C:\\yarn.msi

WORKDIR /bin

COPY ${VULKAN_DLL_PATH} ./vulkan-1.dll

# build scene-lod
FROM base as scene-lod-build

WORKDIR /scene-lod

COPY scene-lod-entities-manifest-builder /scene-lod

RUN npm cache clean --force

COPY scene-lod-entities-manifest-builder/package.json /scene-lod
COPY scene-lod-entities-manifest-builder/package-lock.json /scene-lod

RUN npm ci

RUN npm run build

# build consumer-server
FROM base as consumer-server-build

WORKDIR /consumer-server

COPY consumer-server/package.json /consumer-server/package.json
COPY consumer-server/yarn.lock /consumer-server/yarn.lock
RUN yarn install --frozen-lockfile

COPY consumer-server /consumer-server
RUN yarn build

# build the dotnet app
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as dotnet-build

WORKDIR /app

COPY DCL_PiXYZ/ ./api
COPY nuget.config ./
COPY local_packages/ ./local_packages

WORKDIR /app/api

RUN dotnet publish -c Release -r win10-x64 -o ./publish --self-contained true

COPY DCL_PiXYZ/ ./

# bundle all apps
FROM mcr.microsoft.com/windows/servercore:ltsc2019

ADD https://nodejs.org/dist/v18.0.0/node-v18.0.0-win-x64.zip C:\\node.zip
RUN powershell -Command \
    Expand-Archive -Path C:\\node.zip -DestinationPath C:\\Node; \
    Remove-Item -Force C:\\node.zip

RUN setx /M PATH "%PATH%;C:\Node\node-v18.0.0-win-x64"

WORKDIR /app

COPY --from=base /bin/vulkan-1.dll .

COPY --from=dotnet-build /app/api/publish/DCL_PiXYZ.exe .

COPY --from=scene-lod-build /scene-lod/dist ./scene-lod/dist
COPY --from=scene-lod-build /scene-lod/package.json ./scene-lod/package.json
COPY --from=scene-lod-build /scene-lod/package-lock.json ./scene-lod/package-lock.json
COPY --from=scene-lod-build /scene-lod/node_modules ./scene-lod/node_modules

COPY --from=consumer-server-build /consumer-server/dist ./consumer-server/dist
COPY --from=consumer-server-build /consumer-server/package.json ./consumer-server/package.json
COPY --from=consumer-server-build /consumer-server/yarn.lock ./consumer-server/yarn.lock
COPY --from=consumer-server-build /consumer-server/node_modules ./consumer-server/node_modules
COPY build-lods.ps1 .

CMD ["node", "./consumer-server/dist/index.js"]