// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { window, Uri, workspace } from "vscode";
import { Command } from "./types";
import { LanguageClient } from "vscode-languageclient/node";
import {
  IActionContext,
  parseError,
  UserCancelledError,
} from "@microsoft/vscode-azext-utils";
import path from "path";
import * as fse from "fs-extra";
import {
  BicepGetRecommendedConfigLocationResult,
  createBicepConfigRequestType,
  getRecommendedConfigLocationRequestType,
} from "../language/protocol";
import { getLogger } from "../utils/logger";

const bicepConfig = "bicepconfig.json";

export class CreateBicepConfigurationFile implements Command {
  public readonly id = "bicep.createConfigFile";

  public constructor(private readonly client: LanguageClient) { }

  public async execute(
    _context: IActionContext,
    documentUri?: Uri,
    suppressQuery?: boolean, // If true, the recommended location is used without querying user (for testing)
    rethrow?: boolean // (for testing)
  ): Promise<string | undefined> {
    console.log(`asdfg20: ${documentUri?.toString()}`);
    console.log(`asdfg20: ${documentUri?.fsPath}`);
    console.log("console.log");

    _context.errorHandling.rethrow = !!rethrow;

    documentUri ??= window.activeTextEditor?.document.uri;
    console.log(`asdfg21: ${String(documentUri?.toString())}`);
    console.log(`asdfg21: ${String(documentUri?.fsPath)}`);

    const recommendation: BicepGetRecommendedConfigLocationResult =
      await this.client.sendRequest(getRecommendedConfigLocationRequestType, {
        BicepFilePath: documentUri?.fsPath,
      });
    if (recommendation.error || !recommendation.recommendedFolder) {
      throw new Error(
        `Could not determine recommended configuration location: ${recommendation.error ?? "Unknown"
        }`
      );
    }
    console.log(`asdfg22: ${recommendation.recommendedFolder}`);

    let selectedPath: string = path.join(
      recommendation.recommendedFolder,
      bicepConfig
    );
    console.log(`asdfg23: ${selectedPath}`);
    if (!suppressQuery) {
      // eslint-disable-next-line no-constant-condition
      while (true) {
        const response = await window.showSaveDialog({
          defaultUri: Uri.file(selectedPath),
          filters: { "bicep.config files": [bicepConfig] },
          title: "Where would you like to save the Bicep configuration file?",
          saveLabel: "Save configuration file",
        });
        if (!response || !response.fsPath) {
          throw new UserCancelledError("browse");
        }

        selectedPath = response.fsPath;

        if (path.basename(selectedPath) !== bicepConfig) {
          window.showErrorMessage(
            `A Bicep configuration file must be named ${bicepConfig}`
          );
          selectedPath = path.join(path.dirname(selectedPath), bicepConfig);
        } else {
          break;
        }
      }
    }
    //asdfg good to here
    console.log(`asdfg24: ${selectedPath}`);

    console.log(`selectedPath: ${selectedPath}`);
    let p = selectedPath;
    while (path.dirname(p) !== p) {
      try {
        console.log(`${p}:`);
      } catch (err) {
        console.log(parseError(err).message);
      }
      try {
        console.log(`  exists: ${fse.existsSync(p)}`);
      } catch (err) {
        console.log(parseError(err).message);
      }
      try {
        console.log(`  dir: ${fse.readdirSync(p).join(" | ")}`);
      } catch (err) {
        console.log(parseError(err).message);
      }
      p = path.dirname(p);
      console.log(`asdfg0`);
    }

    //asdfg dies before here
    console.log(`asdfg1`);
    await this.client.sendRequest(createBicepConfigRequestType, {
      destinationPath: selectedPath,
    });
    console.log(`asdfg2`);

    if (await fse.pathExists(selectedPath)) {
      console.log(`asdfg3`);
      const textDocument = await workspace.openTextDocument(selectedPath);
      console.log(`asdfg4`);
      await window.showTextDocument(textDocument);
      console.log(`asdfg5`);
      return selectedPath;
    } else {
      console.log(`asdfg6`);
      throw new Error(
        "Configuration file was not created by the language server"
      );
    }

  }
}
