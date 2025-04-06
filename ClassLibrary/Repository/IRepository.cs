using ClassLibrary.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.Repository;

public interface IRepository
{
    Task AddVideoAsync(Video video);

    Task AddFrameAsync(Frame frame);

    Task AddDetectionAsync(Detection detection);
}
