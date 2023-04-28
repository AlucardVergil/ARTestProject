import os
import argparse

def replace_number_in_files(folder_path, old_number, new_number):
    for filename in os.listdir(folder_path):
        if filename.endswith('.txt'):
            file_path = os.path.join(folder_path, filename)
            with open(file_path, 'r') as file:
                lines = file.readlines()

            with open(file_path, 'w') as file:
                for line in lines:
                    numbers = line.split()
                    if numbers:
                        first_number = numbers[0]
                        if first_number == old_number:
                            numbers[0] = new_number
                            line = ' '.join(numbers) + '\n'
                    file.write(line)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Replace numbers in text files')
    parser.add_argument('-p', '--path', type=str, help='path to the folder containing the text files')
    parser.add_argument('-o', '--old_number', type=str, help='number to be replaced')
    parser.add_argument('-n', '--new_number', type=str, help='new number to replace with')
    args = parser.parse_args()

    folder_path = args.path
    old_number = args.old_number
    new_number = args.new_number

    replace_number_in_files(folder_path, old_number, new_number)
    print('Number replacement completed successfully.')