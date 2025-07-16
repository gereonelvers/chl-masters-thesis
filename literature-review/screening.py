import pandas as pd
import asyncio
import aiohttp
from tqdm import tqdm

# Define constants for rate limiting
MAX_REQUESTS_PER_MINUTE = 2000
REQUESTS_PER_SECOND = MAX_REQUESTS_PER_MINUTE / 60
CONCURRENT_REQUESTS = 30


# Define an asynchronous function to interact with the OpenAI API
async def get_gpt4o_response(session, prompt):
    url = "https://api.openai.com/v1/chat/completions"
    headers = {
        "Authorization": f"Bearer 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",
        "Content-Type": "application/json"
    }
    data = {
        "model": "gpt-4o",  # Model name retained as requested
        "messages": [
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": prompt}
        ],
        "temperature": 0
    }

    async with session.post(url, headers=headers, json=data) as response:
        if response.status == 200:
            response_data = await response.json()
            return response_data["choices"][0]["message"]["content"].strip()
        else:
            return f"Error: {response.status}"

# Async function to process a single row with five prompts
async def process_row(session, row, index, df, semaphore):
    # Extract relevant old-review for prompt construction
    input_value = f"Title: {row['TI']}\n\nAbstract: {row['AB']}"

    # Define prompts based on row information
    prompt_1 = f"Below, see some information about a scientific paper. Based on that information, does the paper deal with AR/XR (augmented reality/mixed reality) at all? Respond with a very short explanation of your answer, then on a new line put a single word (\"yes\"/\"no\"/\"unsure\").\n\n{input_value}"
    prompt_2 = f"Below, see some information about a scientific paper. Based on that information, does the paper deal with collaboration (meaning anything involving more than one person/worker) at all? Respond with a very short explanation of your answer, then on a new line put a single word (\"yes\"/\"no\"/\"unsure\").\n\n{input_value}"
    prompt_3 = f"Below, see some information about a scientific paper. Based on that information, does the paper feature an industrial or manufacturing context at all? Respond with a very short explanation of your answer, then on a new line put a single word (\"yes\"/\"no\"/\"unsure\").\n\n{input_value}"
    prompt_4 = f"Below, see some information about a scientific paper. Based on that information, does the AR setup involve a headset (e.g., HoloLens, VR headset)? Respond with a very short explanation of your answer, then on a new line put a single word (\"yes\"/\"no\"/\"unsure\").\n\n{input_value}"
    prompt_5 = f"Below, see some information about a scientific paper. Based on that information, is the AR/VR experience set in a co-located/same-environment/shared-space setting (as in: not only remote)? Respond with a very short explanation of your answer, then on a new line put a single word (\"yes\"/\"no\"/\"unsure\").\n\n{input_value}"

    async with semaphore:  # Limit the number of concurrent tasks
        response_1 = await get_gpt4o_response(session, prompt_1)
        response_2 = await get_gpt4o_response(session, prompt_2)
        response_3 = await get_gpt4o_response(session, prompt_3)
        response_4 = await get_gpt4o_response(session, prompt_4)
        response_5 = await get_gpt4o_response(session, prompt_5)

    # Update the DataFrame with responses
    df.at[index, 'AR/XR Response'] = response_1
    df.at[index, 'Collaboration Response'] = response_2
    df.at[index, 'Industrial Context Response'] = response_3
    df.at[index, 'AR Device Type Response'] = response_4
    df.at[index, 'AR Setting Response'] = response_5


# Main function to handle parallel processing of the entire DataFrame with rate limiting
async def main():
    # Load the Excel file
    df = pd.read_excel('./literature-baseline-export.xlsx')

    # Create an aiohttp session
    async with aiohttp.ClientSession() as session:
        # Create a semaphore to limit the number of concurrent requests
        semaphore = asyncio.Semaphore(CONCURRENT_REQUESTS)

        # Create a list of tasks for processing rows concurrently
        tasks = []
        for index, row in tqdm(df.iterrows(), total=len(df), desc="Processing Rows"):
            tasks.append(process_row(session, row, index, df, semaphore))

            # Throttle to ensure we do not exceed rate limits
            if len(tasks) % REQUESTS_PER_SECOND == 0:
                await asyncio.sleep(1)  # Ensure we're not sending more than the allowed requests per second

        # Run tasks concurrently
        await asyncio.gather(*tasks)

    # Save the updated DataFrame back to an Excel file
    df.to_excel('./literature-baseline-export-updated.xlsx', index=False)

# Run the async main function
if __name__ == '__main__':
    asyncio.run(main())
