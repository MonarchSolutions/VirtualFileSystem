// Copyright (c) 2022-2023, Atypical Consulting SRL
// All rights reserved... but seriously, we're open to sharing if you ask nicely!
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree. 

using Atypical.VirtualFileSystem.Core.Contracts;
using System.Text.RegularExpressions;

namespace VirtualFileSystem.UnitTests.Models;

public class VirtualFileSystemTests
{
    public class Constructor
    {
        [Fact]
        public void Constructor_creates_a_new_file_system()
        {
            // Act
            IVirtualFileSystem vfs = new VFS();

            // Assert
            vfs.Should().NotBeNull();
            vfs.IsEmpty().Should().BeTrue();
            vfs.Root.IsDirectory.Should().BeTrue();
            vfs.Root.IsFile.Should().BeFalse();
            vfs.Root.Path.Value.Should().Be("vfs://");
            vfs.Root.CreationTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromHours(1));
        }
    }

    public class MethodGetRootPath
    {
        [Fact]
        public void GetRootPath_returns_the_root_path()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();

            // Act
            var rootPath = vfs.GetRootPath();

            // Assert
            rootPath.Should().NotBeNull();
            rootPath.Value.Should().Be("vfs://");
        }
    }
    
    public class MethodGetDirectory
    {
        [Fact]
        public void GetDirectory_returns_the_root_directory()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            var rootPath = new VFSRootPath();

            // Act
            var root = vfs.GetDirectory(rootPath);

            // Assert
            root.Should().NotBeNull();
            root.Path.Value.Should().Be("vfs://");
        }

        [Fact]
        public void GetDirectory_returns_a_directory()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string directoryPath = "dir1/dir2/dir3";
            vfs.CreateDirectory(directoryPath);

            // Act
            var directory = vfs.GetDirectory(directoryPath);

            // Assert
            directory.Should().NotBeNull();
            directory.Path.Value.Should().Be("vfs://dir1/dir2/dir3");
        }

        [Fact]
        public void GetDirectory_throws_an_exception_if_the_directory_does_not_exist()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string directoryPath = "dir1/dir2/dir3";

            // Act
            Action action = () => vfs.GetDirectory(directoryPath);

            // Assert
            action.Should().Throw<KeyNotFoundException>();
        }
    }

    public class MethodTryGetDirectory
    {
        [Fact]
        public void TryGetDirectory_returns_true_if_the_directory_exists()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string directoryPath = "dir1/dir2/dir3";
            vfs.CreateDirectory(directoryPath);

            // Act
            var result = vfs.TryGetDirectory(directoryPath, out var directory);

            // Assert
            result.Should().BeTrue();
            directory.Should().NotBeNull();
            directory!.Path.Value.Should().Be("vfs://dir1/dir2/dir3");
        }

        [Fact]
        public void TryGetDirectory_returns_false_if_the_directory_does_not_exist()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string directoryPath = "dir1/dir2/dir3";

            // Act
            var result = vfs.TryGetDirectory(directoryPath, out var directory);

            // Assert
            result.Should().BeFalse();
            directory.Should().BeNull();
        }
    }

    public class MethodCreateDirectory
    {
        [Fact]
        public void CreateDirectory_creates_a_directory()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string directoryPath = "dir1";

            // Act
            vfs.CreateDirectory(directoryPath);

            // Assert
            vfs.IsEmpty().Should().BeFalse();
            vfs.Index.Should().NotBeEmpty();
            vfs.Index.Should().HaveCount(1);
            vfs.Index.Should().ContainKey("vfs://dir1");
            vfs.Root.IsDirectory.Should().BeTrue();
            vfs.Root.IsFile.Should().BeFalse();
            vfs.Root.Path.Value.Should().Be("vfs://");
            vfs.Root.CreationTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromHours(1));
        }

        [Fact]
        public void CreateDirectory_creates_a_directory_and_its_parents()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string directoryPath = "dir1/dir2/dir3";
            var path = new VFSDirectoryPath(directoryPath);

            // Act
            vfs.CreateDirectory(directoryPath);

            // Assert
            vfs.Index.Should().NotBeEmpty();
            vfs.Index.Should().HaveCount(3); // dir1 + dir2 + dir3
            vfs.Index.Should().ContainKey(path.Value);
            vfs.Index.Should().ContainKey("vfs://dir1");
            vfs.Index.Should().ContainKey("vfs://dir1/dir2");
            vfs.Index.Should().ContainKey("vfs://dir1/dir2/dir3");
            
            vfs.Index["vfs://dir1"].Should().BeAssignableTo<IDirectoryNode>();
            vfs.Index["vfs://dir1/dir2"].Should().BeAssignableTo<IDirectoryNode>();
            vfs.Index["vfs://dir1/dir2/dir3"].Should().BeAssignableTo<IDirectoryNode>();
            
            vfs.Index["vfs://dir1"].As<IDirectoryNode>().Directories.Should().NotBeEmpty();
            vfs.Index["vfs://dir1"].As<IDirectoryNode>().Directories.Should().HaveCount(1);
        }

        [Fact]
        public void CreateDirectory_throws_an_exception_if_the_directory_already_exists()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            var directoryPath = new VFSDirectoryPath("dir1");
            vfs.CreateDirectory(directoryPath);

            // Act
            Action action = () => vfs.CreateDirectory(directoryPath);

            // Assert
            action.Should()
                .Throw<VFSException>()
                .WithMessage($"The node 'vfs://dir1' already exists in the index.");
        }

        [Fact]
        public void CreateDirectory_throws_an_exception_if_the_path_is_not_a_directory()
        {
            // Arrange
            var filePath = new VFSFilePath("dir1/dir2/dir3/file.txt");
            IVirtualFileSystem vfs = new VFS();
            vfs.CreateFile(filePath);

            // Act
            Action action = () => vfs.CreateDirectory(filePath);

            // Assert
            action.Should()
                .Throw<VFSException>()
                .WithMessage("The directory path 'vfs://dir1/dir2/dir3/file.txt' contains a file extension.");
        }
        
        [Fact]
        public void CreateDirectory_throws_an_exception_if_the_path_is_the_root_directory()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();

            // Act
            Action action = () => vfs.CreateDirectory("vfs://");

            // Assert
            action.Should()
                .Throw<VFSException>()
                .WithMessage("Cannot create the root directory.");
        }
    }

    public class MethodDeleteDirectory
    {
        [Fact]
        public void DeleteDirectory_deletes_a_directory()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string directoryPath = "dir1";
            vfs.CreateDirectory(directoryPath);

            // Act
            vfs.DeleteDirectory(directoryPath);

            // Assert
            vfs.Index.Count.Should().Be(0);
        }

        [Fact]
        public void DeleteDirectory_deletes_a_directory_and_its_children()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string directoryPath = "dir1/dir2/dir3";
            vfs.CreateDirectory(directoryPath);

            // Act
            vfs.DeleteDirectory("dir1");

            // Assert
            vfs.Index.Count.Should().Be(0);
        }

        [Fact]
        public void DeleteDirectory_throws_an_exception_if_the_directory_does_not_exist()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string directoryPath = "dir1";

            // Act
            Action action = () => vfs.DeleteDirectory(directoryPath);

            // Assert
            action.Should()
                .Throw<VFSException>()
                .WithMessage("The directory 'vfs://dir1' does not exist in the index.");
        }
        
        [Fact]
        public void DeleteDirectory_throws_an_exception_if_the_path_is_the_root_directory()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();

            // Act
            Action action = () => vfs.DeleteDirectory("vfs://");

            // Assert
            action.Should()
                .Throw<VFSException>()
                .WithMessage("Cannot delete the root directory.");
        }
    }

    public class MethodFindDirectories
    {
        [Fact]
        public void FindDirectories_returns_all_directories()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            vfs.CreateDirectory("dir1");
            vfs.CreateDirectory("dir2");
            vfs.CreateDirectory("dir3");

            // Act
            var directories = vfs.FindDirectories().ToList();

            // Assert
            directories.Should().NotBeEmpty();
            directories.Should().HaveCount(3); // dir1 + dir2 + dir3
            directories.Should().Contain(d => d.Path.Value == "vfs://dir1");
            directories.Should().Contain(d => d.Path.Value == "vfs://dir2");
            directories.Should().Contain(d => d.Path.Value == "vfs://dir3");
        }

        [Fact]
        public void FindDirectories_returns_all_directories_matching_a_pattern()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            vfs.CreateDirectory("dir1");
            vfs.CreateDirectory("dir2");
            vfs.CreateDirectory("dir3");

            // Act
            var regexPattern = new Regex("dir1");
            var directories = vfs.FindDirectories(regexPattern).ToList();

            // Assert
            directories.Should().NotBeEmpty();
            directories.Should().HaveCount(1);
            directories.Should().Contain(d => d.Path.Value == "vfs://dir1");
        }
    }

    public class MethodSelectDirectories
    {
        [Fact]
        public void SelectDirectories_returns_all_directories()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            vfs.CreateDirectory("dir1");
            vfs.CreateDirectory("dir2");
            vfs.CreateDirectory("dir3");

            // Act
            var directories = vfs
                .SelectDirectories(x => x.IsDirectory)
                .ToList();

            // Assert
            directories.Should().NotBeEmpty();
            directories.Should().HaveCount(3); // dir1 + dir2 + dir3
            directories.Should().Contain(d => d.Path.Value == "vfs://dir1");
            directories.Should().Contain(d => d.Path.Value == "vfs://dir2");
            directories.Should().Contain(d => d.Path.Value == "vfs://dir3");
        }
    }

    public class MethodGetFile
    {
        [Fact]
        public void GetFile_returns_the_file()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string filePath = "dir1/dir2/dir3/file.txt";
            vfs.CreateFile(filePath);

            // Act
            var file = vfs.GetFile(filePath);

            // Assert
            file.Should().NotBeNull();
            file.Path.Value.Should().Be("vfs://dir1/dir2/dir3/file.txt");
        }

        [Fact]
        public void GetFile_throws_an_exception_if_the_file_does_not_exist()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string filePath = "dir1/dir2/dir3/file.txt";

            // Act
            Action action = () => vfs.GetFile(filePath);

            // Assert
            action.Should().Throw<KeyNotFoundException>();
        }
    }

    public class MethodTryGetFile
    {
        [Fact]
        public void TryGetFile_returns_the_file()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string filePath = "dir1/dir2/dir3/file.txt";
            vfs.CreateFile(filePath);

            // Act
            var result = vfs.TryGetFile(filePath, out var file);

            // Assert
            result.Should().BeTrue();
            file.Should().NotBeNull();
            file!.Path.Value.Should().Be("vfs://dir1/dir2/dir3/file.txt");
        }

        [Fact]
        public void TryGetFile_returns_false_if_the_file_does_not_exist()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string filePath = "dir1/dir2/dir3/file.txt";

            // Act
            var result = vfs.TryGetFile(filePath, out var file);

            // Assert
            result.Should().BeFalse();
            file.Should().BeNull();
        }
    }

    public class MethodCreateFile
    {
        [Fact]
        public void CreateFile_creates_a_file()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string filePath = "file.txt";

            // Act
            vfs.CreateFile(filePath);
            
            // Assert
            vfs.IsEmpty().Should().BeFalse();
            vfs.Index.Should().NotBeEmpty();
            vfs.Index.Should().HaveCount(1);
            vfs.Index.Should().ContainKey("vfs://file.txt");
            vfs.Root.Files.Should().NotBeEmpty();
            vfs.Root.Files.Should().HaveCount(1);
        }

        [Fact]
        public void CreateFile_creates_a_file_and_its_parents()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            const string filePath = "dir1/dir2/dir3/file.txt";
            
            // Act
            vfs.CreateFile(filePath);
            
            // Assert
            vfs.IsEmpty().Should().BeFalse();
            vfs.Index.Should().NotBeEmpty();
            vfs.Index.Should().HaveCount(4); // dir1 + dir2 + dir3 + file.txt
            vfs.Index.Should().ContainKey("vfs://dir1");
            vfs.Index.Should().ContainKey("vfs://dir1/dir2");
            vfs.Index.Should().ContainKey("vfs://dir1/dir2/dir3");
            vfs.Index.Should().ContainKey("vfs://dir1/dir2/dir3/file.txt");
            vfs.Root.Directories.Should().NotBeEmpty();
            vfs.Root.Directories.Should().HaveCount(1);
        }

        [Fact]
        public void CreateFile_throws_an_exception_if_the_file_already_exists()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            var filePath = new VFSFilePath("dir1/dir2/dir3/file.txt");
            vfs.CreateFile(filePath);

            // Act
            Action action = () => vfs.CreateFile(filePath);

            // Assert
            action.Should()
                .Throw<VFSException>()
                .WithMessage("The node 'vfs://dir1/dir2/dir3/file.txt' already exists in the index.");
        }
    }

    public class MethodDeleteFile
    {
        [Fact]
        public void DeleteFile_deletes_a_file()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            var filePath = new VFSFilePath("dir1/dir2/dir3/file.txt");
            vfs.CreateFile(filePath);

            // Act
            vfs.DeleteFile(filePath);

            // Assert
            vfs.Index.Count.Should().Be(3); // dir1, dir2, dir3
        }

        [Fact]
        public void DeleteFile_throws_an_exception_if_the_file_does_not_exist()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();

            // Act
            Action action = () => vfs.DeleteFile("dir1/dir2/dir3/file.txt");

            // Assert
            action.Should()
                .Throw<VFSException>()
                .WithMessage("The file 'vfs://dir1/dir2/dir3/file.txt' does not exist in the index.");
        }
    }

    public class MethodFindFiles
    {
        [Fact]
        public void FindFiles_returns_all_files()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            vfs.CreateDirectory("dir1");
            vfs.CreateDirectory("dir2");
            vfs.CreateDirectory("dir3");
            vfs.CreateFile("dir1/file1.txt");
            vfs.CreateFile("dir2/file2.txt");
            vfs.CreateFile("dir3/file3.txt");

            // Act
            var files = vfs.FindFiles().ToList();

            // Assert
            files.Should().NotBeEmpty();
            files.Should().HaveCount(3);
            files.Should().Contain(f => f.Path.Value == "vfs://dir1/file1.txt");
            files.Should().Contain(f => f.Path.Value == "vfs://dir2/file2.txt");
            files.Should().Contain(f => f.Path.Value == "vfs://dir3/file3.txt");
        }

        [Fact]
        public void FindFiles_with_valid_data_returns_a_list_of_files_with_content_and_name()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();
            vfs.CreateFile("file1.txt", "content1");
            vfs.CreateFile("file2.txt", "content2");
            vfs.CreateFile("file3.txt", "content3");

            var regex = new Regex(@"file\d.txt");

            // Act
            var files = vfs.FindFiles(regex).ToList();

            // Assert
            files.Should().NotBeNull();
            files.Count.Should().Be(3);
            files[0].Name.Should().Be("file1.txt");
            files[0].Content.Should().Be("content1");
            files[1].Name.Should().Be("file2.txt");
            files[1].Content.Should().Be("content2");
            files[2].Name.Should().Be("file3.txt");
            files[2].Content.Should().Be("content3");
            // Assert Index
            vfs.Index.Count.Should().Be(3); // file1, file2, file3
        }
    }

    public class MethodGetTree
    {
        [Fact]
        public void GetTree_returns_root_directory()
        {
            // Arrange
            IVirtualFileSystem vfs = new VFS();

            // Act
            var result = vfs.GetTree();

            // Assert
            result.Should().Be("vfs://");
        }

        [Fact]
        public void GetTree_returns_3_files_as_ASCII_tree()
        {
            // Arrange
            var expected = """
                vfs://
                ├── file1.txt
                ├── file2.txt
                └── file3.txt
                """.ReplaceLineEndings();

            var vfs = new VFS()
                .CreateFile("file1.txt")
                .CreateFile("file2.txt")
                .CreateFile("file3.txt");

            // Act
            var result = vfs.GetTree();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetTree_returns_3_directories_as_ASCII_tree()
        {
            // Arrange
            var expected = """
                vfs://
                ├── dir1
                ├── dir2
                └── dir3
                """.ReplaceLineEndings();

            var vfs = new VFS()
                .CreateDirectory("dir1")
                .CreateDirectory("dir2")
                .CreateDirectory("dir3");

            // Act
            var result = vfs.GetTree();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetTree_returns_3_files_and_3_directories_as_ASCII_tree()
        {
            // Arrange
            var expected = """
                vfs://
                ├── dir1
                │   ├── file1.txt
                │   ├── file2.txt
                │   └── file3.txt
                ├── dir2
                │   ├── file1.txt
                │   ├── file2.txt
                │   └── file3.txt
                └── dir3
                    ├── file1.txt
                    ├── file2.txt
                    └── file3.txt
                """.ReplaceLineEndings();

            var vfs = new VFS()
                .CreateFile("dir1/file1.txt")
                .CreateFile("dir1/file2.txt")
                .CreateFile("dir1/file3.txt")
                .CreateFile("dir2/file1.txt")
                .CreateFile("dir2/file2.txt")
                .CreateFile("dir2/file3.txt")
                .CreateFile("dir3/file1.txt")
                .CreateFile("dir3/file2.txt")
                .CreateFile("dir3/file3.txt");

            // Act
            var result = vfs.GetTree();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetTree_returns_a_complex_tree()
        {
            // Arrange
            var expected = """
                vfs://
                ├── dir1
                │   ├── dir2
                │   │   ├── dir3
                │   │   │   ├── file1.txt
                │   │   │   ├── file2.txt
                │   │   │   └── file3.txt
                │   │   ├── file1.txt
                │   │   ├── file2.txt
                │   │   └── file3.txt
                │   ├── file1.txt
                │   ├── file2.txt
                │   └── file3.txt
                ├── dir2
                │   ├── dir3
                │   │   ├── file1.txt
                │   │   ├── file2.txt
                │   │   └── file3.txt
                │   ├── file1.txt
                │   ├── file2.txt
                │   └── file3.txt
                └── dir3
                    ├── file1.txt
                    ├── file2.txt
                    └── file3.txt
                """.ReplaceLineEndings();

            var vfs = new VFS()
                .CreateFile("dir1/dir2/dir3/file1.txt")
                .CreateFile("dir1/dir2/dir3/file2.txt")
                .CreateFile("dir1/dir2/dir3/file3.txt")
                .CreateFile("dir1/dir2/file1.txt")
                .CreateFile("dir1/dir2/file2.txt")
                .CreateFile("dir1/dir2/file3.txt")
                .CreateFile("dir1/file1.txt")
                .CreateFile("dir1/file2.txt")
                .CreateFile("dir1/file3.txt")
                .CreateFile("dir2/dir3/file1.txt")
                .CreateFile("dir2/dir3/file2.txt")
                .CreateFile("dir2/dir3/file3.txt")
                .CreateFile("dir2/file1.txt")
                .CreateFile("dir2/file2.txt")
                .CreateFile("dir2/file3.txt")
                .CreateFile("dir3/file1.txt")
                .CreateFile("dir3/file2.txt")
                .CreateFile("dir3/file3.txt");

            // Act
            var result = vfs.GetTree();

            // Assert
            result.Should().Be(expected);
        }
    }

    public class MethodToString
    {
        [Fact]
        public void ToString_returns_a_summary_of_the_VFS()
        {
            // Arrange
            const string expected = "VFS: 3 files, 3 directories";

            var vfs = new VFS()
                .CreateFile("file1.txt")
                .CreateFile("file2.txt")
                .CreateFile("file3.txt")
                .CreateDirectory("dir1")
                .CreateDirectory("dir2")
                .CreateDirectory("dir3");

            // Act
            var result = vfs.ToString();

            // Assert
            result.Should().Be(expected);
        }
    }
}
