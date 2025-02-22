const core = require('@actions/core');
const github = require('@actions/github');
const axios = require('axios');
import OpenAI from 'openai';

const client = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY, // This is the default and can be omitted
});

async function run() {
  try {
    const openaiApiKey = process.env.OPENAI_API_KEY;
    const githubToken = process.env.GITHUB_TOKEN;

    if (!openaiApiKey) {
      throw new Error("Missing OpenAI API key");
    }

    // Obter o número do pull request
    const prNumber = github.context.payload.pull_request.number;
    core.info(prNumber);
    
    // Aqui você pode usar a API do GitHub para obter o diff dos arquivos alterados.
    // Por simplicidade, usaremos um exemplo estático. Em uma implementação real, você
    // faria algo como: octokit.rest.pulls.listFiles({ ... }) para coletar os diffs.
    const codeDiff = "Example diff content...";

    // Monta o prompt para enviar ao ChatGPT
    const prompt = `Please review the following code diff and provide detailed feedback:\n\n${codeDiff}`;

    // Chamada à API do OpenAI
    const response = await axios.post('https://api.openai.com/v1/chat/completions', {
      model: "gpt-3.5-turbo",
      messages: [{ role: "user", content: prompt }],
    }, {
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${openaiApiKey}`
      }
    });

    core.info(`Response: ${response}`);

    const reviewFeedback = response.data.choices[0].message.content;

    // Postar o feedback como comentário no pull request
    const octokit = github.getOctokit(githubToken);
    core.info(JSON.stringify(octokit));
    await octokit.rest.issues.createComment({
      owner: github.context.repo.owner,
      repo: github.context.repo.repo,
      issue_number: prNumber,
      body: `### ChatGPT Code Review Feedback:\n\n${reviewFeedback}`
    });

    core.info("Feedback posted successfully.");
  } catch (error) {
    core.setFailed(`Action failed with error ${error.message}`);
    core.setFailed(`Error detail ${JSON.stringify(error)}`)
  }
}

run();
