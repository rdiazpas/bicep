// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
  IActionContext,
  IAzureQuickPickItem,
  IAzureUserInput,
  TelemetryProperties,
  DialogResponses,
  UserCancelledError,
} from "@microsoft/vscode-azext-utils";
import * as path from "path";
import * as os from "os";
import * as fse from "fs-extra";
import { TextDocument, Uri, window, workspace } from "vscode";
import { getLogger } from "../utils";

type TargetFile =
  | "rightClick"
  | "activeEditor"
  | "noWorkspaceActiveEditor"
  | "singleBicepFile"
  | "fromWorkspace"
  | "new";
type Properties = TelemetryProperties & { targetFile: TargetFile };

// Throws user-cancelled on cancel
export async function findOrCreateActiveBicepFile(
  context: IActionContext,
  documentUri: Uri | undefined,
  prompt: string,
  options?: {
    considerActiveEditor?: boolean;
  }
): Promise<Uri> {
  getLogger().debug(`=====================`);
  getLogger().debug(
    `findOrCreateActiveBicepFile: "${prompt}", documentUri=${documentUri}`
  );
  getLogger().debug(`prompt: ${prompt}`);
  getLogger().debug(`documentUri: ${String(documentUri?.toString())}`);
  getLogger().debug(
    `workspace.workspaceFile: ${String(workspace.workspaceFile?.toString())}`
  );
  getLogger().debug(`folders: ${String(workspace.workspaceFolders)}`);
  getLogger().debug(
    `folders.length: ${String(workspace.workspaceFolders?.length)}`
  );
  if (workspace.workspaceFolders && workspace.workspaceFolders.length > 0) {
    getLogger().debug(
      `folders[0]: ${String(
        (workspace.workspaceFolders ?? [])[0].uri.toString()
      )}`
    );
  }

  const properties = <Properties>context.telemetry.properties;
  const ui = context.ui;

  if (documentUri) {
    getLogger().debug("asdfg7");
    properties.targetFile = "rightClick";
    getLogger().debug(properties.targetFile);
    return documentUri;
  }

  if (options?.considerActiveEditor) {
    getLogger().debug("asdfg8");
    const activeEditor = window.activeTextEditor;
    if (activeEditor?.document?.languageId === "bicep") {
      properties.targetFile = "activeEditor";
      getLogger().debug("asdfg9");
      getLogger().debug(properties.targetFile);
      return activeEditor.document.uri;
    }
  }

  const bicepFilesInWorkspace = (
    await workspace.findFiles("**/*.bicep", undefined)
  ).filter((f) => !!f.fsPath);

  // If there's only a single Bicep file in the workspace, always use that
  if (bicepFilesInWorkspace.length === 1) {
    properties.targetFile = "singleBicepFile";
    getLogger().debug(properties.targetFile);
    getLogger().debug("asdfg10");
    return bicepFilesInWorkspace[0];
  }

  getLogger().debug("asdfg1");
  // If there are no Bicep files in the workspace...
  if (bicepFilesInWorkspace.length === 0) {
    getLogger().debug("asdfg2");
    if (!workspace.workspaceFolders) {
      getLogger().debug("asdfg3");
      // If there is no workspace open, check the active editor
      const activeEditor = window.activeTextEditor;
      if (activeEditor?.document?.languageId === "bicep") {
        properties.targetFile = "noWorkspaceActiveEditor";
        getLogger().debug(properties.targetFile);
        getLogger().debug("asdfg4");
        return activeEditor.document.uri;
      }
    }

    // Otherwise ask to create one...
    return await queryCreateBicepFile(ui, properties);
  }

  const entries: IAzureQuickPickItem<Uri>[] = bicepFilesInWorkspace.map((u) => {
    const workspaceRoot: string | undefined =
      workspace.getWorkspaceFolder(u)?.uri.fsPath;
    const relativePath = workspaceRoot
      ? path.relative(workspaceRoot, u.fsPath)
      : path.basename(u.fsPath);

    return <IAzureQuickPickItem<Uri>>{
      label: relativePath,
      data: u,
    };
  });

  const response = await ui.showQuickPick(entries, {
    placeHolder: prompt,
  });
  properties.targetFile = "fromWorkspace";
  return response.data;
}

async function queryCreateBicepFile(
  ui: IAzureUserInput,
  properties: Properties
): Promise<Uri> {
  await ui.showWarningMessage(
    "Couldn't find any Bicep files in your workspace. Would you like to create a Bicep file?",
    DialogResponses.yes,
    DialogResponses.cancel
  );

  // User said yes (otherwise would have thrown user cancel error)
  const startingFolder: Uri =
    (workspace.workspaceFolders
      ? workspace.workspaceFolders[0].uri
      : undefined) ?? Uri.file(os.homedir());
  const uri: Uri | undefined = await window.showSaveDialog({
    title: "Save new Bicep file",
    defaultUri: Uri.joinPath(startingFolder, "main"),
    filters: { "Bicep files": ["bicep"] },
  });
  if (!uri) {
    throw new UserCancelledError("saveDialog");
  }

  const path = uri.fsPath;
  if (!path) {
    throw new Error(`Can't save file to location ${uri.toString()}`);
  }

  properties.targetFile = "new";
  await fse.writeFile(
    path,
    "@description('Location of all resources')\nparam location string = resourceGroup().location\n",
    { encoding: "utf-8" }
  );

  const document: TextDocument = await workspace.openTextDocument(uri);
  await window.showTextDocument(document);

  return uri;
}
